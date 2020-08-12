
delete_service()
{
    service="$1" 
    systemctl stop $service
    systemctl disable $service
    rm /etc/systemd/system/$service
    rm /etc/systemd/system/$service # and symlinks that might be related
    rm /usr/lib/systemd/system/$service 
    rm /usr/lib/systemd/system/$service # and symlinks that might be related
    systemctl daemon-reload
    systemctl reset-failed
}

delete_service "mssql-server.service"
delete_service "caddy.service"
delete_service "moonglade.service"

rm ~/apps/moongladeApp -rvf
rm ~/Moonglade -rvf
rm /etc/caddy -rvf
rm /var/opt/mssql/ -rvf

apt remove caddy -y
apt remove mssql-server -y

echo "Successfully uninstalled Kahla on your machine!"