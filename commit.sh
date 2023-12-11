AZUSERNAME=$AZUSERNAME
AZUSER_EMAIL=$AZUSER_EMAIL
AZORG=$AZORG
GHUSER=$GHUSER
GHPAT=$GHPAT

git config --global user.email "$AZUSER_EMAIL"
git config --global user.name "$AZUSERNAME"

git clone https://$GHUSER:$GHPAT@github.com/$GHUSER/Moonglade ./Moonglade-gh
GIT_CMD_REPOSITORY="https://$AZUSERNAME:$AZUREPAT@dev.azure.com/$AZORG/Moonglade/_git/Moonglade"
git clone $GIT_CMD_REPOSITORY ./Moonglade-az-develop
git clone $GIT_CMD_REPOSITORY ./Moonglade-az-master

echo "Checking out develop"
pushd Moonglade-az-develop
git checkout develop
rm -rf .git
popd

echo "Checking out master"
pushd Moonglade-az-master
git checkout master
rm -rf .git
popd

echo "Copying new stuff from Azure dev to Github"
cp -r Moonglade-az-develop/* Moonglade-gh/

echo "Checking out master and add new stuff"
pushd Moonglade-gh
git checkout develop
git add .
git commit -m "sync dev from azure to github"
git push
popd

pushd Moonglade-gh
git checkout master
echo "Copying new stuff from Azure dev to Github"
cp -r Moonglade-az-master/* .
git add .
git commit -m "sync master from azure to github"
git push
popd
