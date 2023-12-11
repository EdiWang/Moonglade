AZUREPAT=$AZUREPAT
AZUSERNAME=$AZUSERNAME
AZUSER_EMAIL=$AZUSER_EMAIL
AZORG=$AZORG
<<<<<<< HEAD
git clone https://github.com/saigkill/Moonglade ./Moonglade-gh-master
git clone https://github.com/saigkill/Moonglade ./Moonglade-gh-develop

cd Moonglade-gh-master
rm -rf .git

cd Moonglade-gh-develop
git checkout develop
rm -rf .git

cd ..

GIT_CMD_REPOSITORY="https://$AZUSERNAME:$AZUREPAT@dev.azure.com/$AZORG/Moonglade/_git/Moonglade"
git clone $GIT_CMD_REPOSITORY ./Moonglade-az
cp -r Moonglade-gh-master/* Moonglade-az/

pushd Moonglade-az
=======
GHUSER=$GHUSER
GHPAT=$GHPAT
>>>>>>> develop

git config --global user.email "$AZUSER_EMAIL"
git config --global user.name "$AZUSERNAME"

<<<<<<< HEAD
git add .
git commit -m "sync prod from git to azure"
git push

git checkout develop

popd

cp -r Moonglade-gh-develop/* Moonglade-az/

pushd Moonglade-az

git add .
git commit -m "sync dev from git to azure"
git push

popd
=======
git clone https://$GHUSER:$GHPAT@github.com/saigkill/Moonglade ./Moonglade-gh
GIT_CMD_REPOSITORY="https://$AZUSERNAME:$AZUREPAT@dev.azure.com/$AZORG/Moonglade/_git/Moonglade"
git clone $GIT_CMD_REPOSITORY ./Moonglade-az

cd Moonglade-az
rm -rf .git
cd ..

cp -r Moonglade-az/* Moonglade-gh/

pushd Moonglade-gh

git add .
git commit -m "sync prod from azure to github"
git push

popd
>>>>>>> develop
