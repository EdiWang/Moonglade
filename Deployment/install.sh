
enable_bbr()
{
    enable_bbr_force()
    {
        echo "BBR not enabled. Enabling BBR..."
        echo 'net.core.default_qdisc=fq' | tee -a /etc/sysctl.conf
        echo 'net.ipv4.tcp_congestion_control=bbr' | tee -a /etc/sysctl.conf
        sysctl -p
    }
    sysctl net.ipv4.tcp_available_congestion_control | grep -q bbr ||  enable_bbr_force
}

set_production()
{
    cat /etc/environment | grep -q "ASPNETCORE_ENVIRONMENT" || echo 'ASPNETCORE_ENVIRONMENT="Production"' | tee -a /etc/environment
    cat /etc/environment | grep -q "DOTNET_CLI_TELEMETRY_OPTOUT" || echo 'DOTNET_CLI_TELEMETRY_OPTOUT="1"' | tee -a /etc/environment
    cat /etc/environment | grep -q "DOTNET_PRINT_TELEMETRY_MESSAGE" || echo 'DOTNET_PRINT_TELEMETRY_MESSAGE="false"' | tee -a /etc/environment
    cat /etc/environment | grep -q "ASPNETCORE_FORWARDEDHEADERS_ENABLED" || echo 'ASPNETCORE_FORWARDEDHEADERS_ENABLED="true"' | tee -a /etc/environment
    export DOTNET_PRINT_TELEMETRY_MESSAGE="false"
    export ASPNETCORE_ENVIRONMENT="Production"
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export ASPNETCORE_FORWARDEDHEADERS_ENABLED="true"
}

get_port()
{
    while true; 
    do
        local PORT=$(shuf -i 40000-65000 -n 1)
        ss -lpn | grep -q ":$PORT " || echo $PORT && break
    done
}

open_port()
{
    port_to_open="$1"
    if [[ "$port_to_open" == "" ]]; then
        echo "You must specify a port!'"
        return 9
    fi

    ufw allow $port_to_open/tcp
    ufw reload
}

enable_firewall()
{
    open_port 22
    echo "y" | ufw enable
    echo "Firewall enabled!"
    ufw status
}

add_caddy_proxy()
{
    domain_name="$1"
    local_port="$2"
    cat /etc/caddy/Caddyfile | grep -q "an easy way" && echo "" > /etc/caddy/Caddyfile
    echo "
$domain_name {
    reverse_proxy /* 127.0.0.1:$local_port
}" >> /etc/caddy/Caddyfile
    systemctl restart caddy.service
}

register_service()
{
    service_name="$1"
    local_port="$2"
    run_path="$3"
    dll="$4"
    echo "[Unit]
    Description=$dll Service
    After=network.target
    Wants=network.target

    [Service]
    Type=simple
    ExecStart=/usr/bin/dotnet $run_path/$dll.dll --urls=http://localhost:$local_port/
    WorkingDirectory=$run_path
    Restart=always
    RestartSec=10
    KillSignal=SIGINT

    [Install]
    WantedBy=multi-user.target" > /etc/systemd/system/$service_name.service
    systemctl enable $service_name.service
    systemctl start $service_name.service
}

add_source()
{
    # dotnet
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -r -s)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb && rm ./packages-microsoft-prod.deb
    # caddy
    cat /etc/apt/sources.list.d/caddy-fury.list | grep -q caddy || echo "deb [trusted=yes] https://apt.fury.io/caddy/ /" | tee -a /etc/apt/sources.list.d/caddy-fury.list
    # sql server
    curl -s https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
    add-apt-repository "$(curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -r -s)/mssql-server-2019.list)"
    # node js
    curl -sL https://deb.nodesource.com/setup_14.x | sudo -E bash -
}

update_settings()
{
    key="$1"
    value="$2"
    path="$3"
    dbFixedString=$(echo "\"$key\": \"$value\"")
    dbLineNumber=$(grep -n \"$key\" $path/appsettings.Production.json | cut -d : -f 1)
    pattern=$(echo $dbLineNumber)s/.*/$dbFixedString/
    sed -i "$pattern" $path/appsettings.Production.json
}

install_Moonglade()
{
    server="$1"
    echo "Installing Moonglade to domain $server..."

    # Valid domain is required
    if [[ $(curl -sL ifconfig.me) == "$(dig +short $server)" ]]; 
    then
        echo "IP is correct."
    else
        echo "$server is not your current machine IP!"
        return 9
    fi

    port=$(get_port)
    dbPassword=$(uuidgen)
    echo "Using internal port: 127.0.0.1:$port to run the internal service."

    cd ~

    # Enable BBR
    enable_bbr

    # Set production mode
    set_production

    # Install basic packages
    echo "Installing git vim dotnet-sdk caddy mssql-server mssql-tools nodejs ufw libgdiplus..."
    add_source > /dev/null
    ACCEPT_EULA=Y apt install -y apt-transport-https git vim dotnet-sdk-3.1 caddy mssql-server mssql-tools unixodbc-dev nodejs ufw libgdiplus > /dev/null

    # Init database password
    MSSQL_SA_PASSWORD=$dbPassword MSSQL_PID='express' /opt/mssql/bin/mssql-conf -n setup accept-eula
    systemctl restart mssql-server

    # Download the source code
    ls | grep -q Moonglade && rm ./Moonglade -rf
    mkdir Storage
    chmod -R 777 ~/Storage/
    git clone -b master https://github.com/EdiWang/Moonglade.git

    # Build the code
    echo 'Building the source code...'
    moonglade_path="$(pwd)/apps/moongladeApp"
    #rm ./Moonglade/src/Moonglade.Web/libman.json # Remove libman because it is easy to crash.
    dotnet publish -c Release -o $moonglade_path -r linux-x64 /p:PublishReadyToRun=true --no-self-contained ./Moonglade/src/Moonglade.Web/Moonglade.Web.csproj
    rm ~/Moonglade -rf
    cat $moonglade_path/appsettings.json > $moonglade_path/appsettings.Production.json

    # Configure appsettings.json
    echo 'Generating default configuration file...'
    connectionString="Server=tcp:127.0.0.1,1433;Initial Catalog=Moonglade;Persist Security Info=False;User ID=sa;Password=$dbPassword;MultipleActiveResultSets=True;Connection Timeout=30;"
    update_settings "MoongladeDatabase" "$connectionString" $moonglade_path
    update_settings Path '\/root\/Storage' $moonglade_path
    npm install web-push -g

    # Create database.
    echo 'Creating database...'
    echo 'Create Database Moonglade' > ~/initDb.sql
    /opt/mssql-tools/bin/sqlcmd -U sa -P $dbPassword -S 127.0.0.1 -i ~/initDb.sql

    # Register Moonglade service
    echo "Registering Moonglade service..."
    register_service "moonglade" $port $moonglade_path "Moonglade.Web"
    sleep 2

    # Config caddy
    echo 'Configuring the web proxy...'
    add_caddy_proxy $server $port
    sleep 2

    # Config firewall
    echo 'Configuring the firewall...'
    open_port 443
    open_port 80
    enable_firewall
    sleep 2

    # Finish the installation
    echo "Successfully installed Moonglade as a service in your machine! Please open https://$server to try it now!"
    echo "Successfully installed mssql as a service in your machine! The port is not opened so you can't connect!"
    echo "Successfully installed caddy as a service in your machine!"
    sleep 1
    echo "You can open your database to public via: sudo ufw allow 1433/tcp"
    echo "You can access your database via: $server:1433 with username: sa and password: $dbPassword"
    echo "Your database data file is located at: /var/opt/mssql/. Please back up them regularly."
    echo "Your web data file is located at: $moonglade_path"
    echo "Your web server config file is located at: /etc/caddy/Caddyfile"
    echo "Strongly maintain your own configuration at $moonglade_path/appsettings.Production.json"
    echo "Strongly suggest run 'sudo apt upgrade' and reboot when convience!"
}

install_Moonglade "$@"
