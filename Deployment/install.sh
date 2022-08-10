aiur() { arg="$( cut -d ' ' -f 2- <<< "$@" )" && curl -sL https://github.com/AiursoftWeb/AiurScript/raw/master/$1.sh | sudo bash -s $arg; }

install_Moonglade()
{

    aiur console/success "Updating..."
    apt-get update --allow-releaseinfo-change
    apt upgrade -y
    server="$1"
    
    aiur console/success "Checking..."
    # Valid domain is required
    if [[ $(curl -sL ifconfig.me) == "$(dig +short $server)" ]]; 
    then
        echo "IP is correct."
    else
        echo "$server is not your current machine IP!"
        return 9
    fi

    port=$(aiur network/get_port)
    dbPassword=$(uuidgen)
    
    cd ~

    # Enable BBR
    aiur network/enable_bbr

    # Install basic packages
    aiur console/success "Installing..."

    apt install -y git vim ufw
    aiur install/jq
    aiur install/dotnet
    aiur install/caddy
    aiur install/sql_server $dbPassword
    #aiur install/node

    aiur console/success "Cloning..."
    ls | grep -q Moonglade && rm ./Moonglade -rf
    mkdir Storage
    chmod -R 777 ~/Storage/
    git clone -b release https://github.com/EdiWang/Moonglade.git

    # Build the code
    aiur console/success 'Building...'
    moonglade_path="$(pwd)/apps/moongladeApp"
    #rm ./Moonglade/src/Moonglade.Web/libman.json # Remove libman because it is easy to crash.
    dotnet publish -c Release -o $moonglade_path -r linux-x64 --no-self-contained ./Moonglade/src/Moonglade.Web/Moonglade.Web.csproj
    cp ~/Moonglade/build/OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf
    rm ~/Moonglade -rf
    cat $moonglade_path/appsettings.json > $moonglade_path/appsettings.Production.json

    # Configure appsettings.json
    aiur console/success 'Configuring...'
    connectionString="Server=tcp:127.0.0.1,1433;Database=Moonglade;uid=sa;Password=$dbPassword;MultipleActiveResultSets=True;"

    aiur text/edit_json "ConnectionStrings.MoongladeDatabase" "$connectionString" $moonglade_path/appsettings.Production.json
    aiur text/edit_json "ImageStorage.FileSystemPath" '\/root\/Storage' $moonglade_path/appsettings.Production.json
    aiur text/edit_json "ImageStorage.FileSystemSettings.Path" '\/root\/Storage' $moonglade_path/appsettings.Production.json
        
    #npm install web-push -g

    # Create database.
    aiur console/success 'Seeding...'
    aiur mssql/create_db "Moonglade" $dbPassword

    # Register Moonglade service
    aiur console/success "Registering..."
    aiur services/register_aspnet_service "moonglade" $port $moonglade_path "Moonglade.Web"
    sleep 2

    # Config caddy
    aiur console/success 'Proxying...'
    aiur caddy/add_proxy $server $port
    sleep 2

    # Config firewall
    aiur console/success 'Porting...'
    aiur firewall/open_port 443
    aiur firewall/open_port 80
    aiur firewall/enable_firewall
    sleep 2

    aiur console/success 'Finlization...'
    # Finish the installation
    echo "Successfully installed Moonglade as a service in your machine! Please open https://$server to try it now!"
    echo "Default management user name is "admin" and default password is "admin123". Please open https://$server/admin to try it now!"
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
