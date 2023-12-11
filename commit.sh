AZUREPAT=$AZUREPAT
AZUSERNAME=$AZUSERNAME
AZUSER_EMAIL=$AZUSER_EMAIL
AZORG=$AZORG
git clone https://github.com/saigkill/Moonglade ./Moonglade-gh-master
git clone https://github.com/saigkill/Moonglade ./Moonglade-gh-develop

cd Moonglade-gh-master
rm -rf .git
cd ..

cd Moonglade-gh-develop
git checkout develop
rm -rf .git
cd ..

GIT_CMD_REPOSITORY="https://$AZUSERNAME:$AZUREPAT@dev.azure.com/$AZORG/Moonglade/_git/Moonglade"
git clone $GIT_CMD_REPOSITORY ./Moonglade-az

cp -r Moonglade-gh-master/* Moonglade-az/

cd Moonglade-az

git config --global user.email "$AZUSER_EMAIL"
git config --global user.name "$AZUSERNAME"

git add .
git commit -m "sync prod from git to azure"
git push

git checkout develop

cd ..

cp -r Moonglade-gh-develop/* Moonglade-az/

cd Moonglade-az

git add .
git commit -m "sync dev from git to azure"
git push

cd ..

echo "Finished Sync"
