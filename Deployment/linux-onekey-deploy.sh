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

