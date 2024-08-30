#!/usr/bin/env bash

export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
stty erase ^?

cd "$(
    cd "$(dirname "$0")" || exit
    pwd
)" || exit

Green="\033[32m"
Red="\033[31m"
Yellow="\033[33m"
Blue="\033[36m"
Font="\033[0m"
GreenBG="\033[42;37m"
RedBG="\033[41;37m"
OK="${Green}[OK]${Font}"
ERROR="${Red}[ERROR]${Font}"

# Helper functions

function print_ok() {
    echo -e "${OK} ${Blue} $1 ${Font}"
}

function print_error() {
    echo -e "${ERROR} ${RedBG} $1 ${Font}"
}

function is_root() {
    if [[ 0 == "$UID" ]]; then
        print_ok "Running as root"
    else
        print_error "Current user is not root, please run as root"
        exit 1
    fi
}

judge() {
    if [[ 0 -eq $? ]]; then
        print_ok "$1 OK"
        sleep 1
    else
        print_error "$1 BOOM BOOM"
        exit 1
    fi
}

function port_exist_check() {
    if ! command -v lsof; then
        apt install lsof
    fi

    if [[ 0 -eq $(lsof -i:"$1" | grep -i -c "listen") ]]; then
        print_ok "$1 port is not used"
        sleep 1
    else
        print_error "Shit! $1 port is used by $1"
        lsof -i:"$1"
        print_error "Killing the process in 5s"
        sleep 5
        lsof -i:"$1" | awk '{print $2}' | grep -v "PID" | xargs kill -9
        print_ok "killed"
        sleep 1
    fi
}

# Logic

function install_common() {
    # curl
    if ! command -v curl; then
        apt install curl -y
    fi

    # Docker
    if ! command -v docker; then
        curl -fsSL https://get.docker.com | sh
        judge "Install docker"
    fi
}

function install_sqlexpress() {
    container_status=$(docker inspect -f '{{.State.Status}}' sqlexpress 2>/dev/null)

    if [ "$container_status" == "running" ]; then
        print_ok "SQL Server Express container is already running."
        return
    elif [ "$container_status" == "exited" ]; then
        echo "SQL Server Express container exists but is stopped. Starting it..."
        docker start sqlexpress
        judge "Start existing SQL Server Express Docker container"
        return
    fi

    mkdir /var/opt/mssql
    judge "Create directory /var/opt/mssql"

    docker run \
        --restart unless-stopped \
        -e "ACCEPT_EULA=Y" \
        -e 'MSSQL_PID=Express' \
        -e "SA_PASSWORD=Work@996" \
        -p 1433:1433 \
        --name sqlexpress \
        -h sqlexpress \
        -v /var/opt/mssql/data:/var/opt/mssql/data \
        -v /var/opt/mssql/log:/var/opt/mssql/log \
        -v /var/opt/mssql/secrets:/var/opt/mssql/secrets \
        -d mcr.microsoft.com/mssql/server:2022-latest

    judge "Run SQL Server Express Docker container"

    sleep 2

    chmod 777 -R /var/opt/mssql
    judge "Change permissions to 777 for /var/opt/mssql"

    print_ok "SQL Server Express installed successfully"
}

function install_moonglade() {
    install_common
    install_sqlexpress
}

function uninstall_moonglade() {
    exit 0
}

menu() {
    is_root
    echo -e "One key Moonglade Deployment Script for Linux VM"
    echo -e "—— ${Yellow}Setup${Font} ——-------------————————————-------------------------------"
    echo -e "${Green}0.${Font}  Check update for this script"
    echo -e "${Green}1.${Font}  Install Moonglade (ASP.NET Core + SQL Server Express on Docker)"
    echo -e "${Green}2.${Font}  Remove Moonglade (Including all your data)"
    echo -e "——-----------------------------------------------------------------"
    echo -e "${Green}30.${Font} Exit"
    read -rp "Enter menu id:" menu_num
    case $menu_num in
    0)
        update_sh
        ;;
    1)
        install_moonglade
        ;;
    2)
        uninstall_moonglade
        ;;
    30)
        exit 0
        ;;
    *)
        print_error "Enter correct number"
        ;;
    esac
}

menu "$@"
