
sudo apt-get update -y

# sudo apt-get install -y linux-headers-`uname -r`
# checking for virtual box guest addidions
# modinfo vboxguest


echo "checking if git is installed..."
if [[ -f "/usr/bin/git" ]]; then
	echo "git already installed,skipping..."
else
	echo "installing git..."
	#https://www.digitalocean.com/community/tutorials/how-to-install-git-on-ubuntu-14-04
	sudo add-apt-repository ppa:git-core/ppa -y
	sudo apt-get update -y
	sudo apt-get install git -y
fi


echo "checking if xfce4desktop is installed..."
if [[ -f "/usr/bin/xfdesktop" ]]; then
	echo "xfdesktop already installed,skipping..."
else
	echo "installing xfdesktop..."
	#sudo dpkg --configure -a	
	sudo sudo apt-get install xubuntu-desktop -y
fi


echo "checking if chrome is installed..."
if [[ -f "/opt/google/chrome/chrome" ]]; then
	echo "chrome already installed,skipping..."
else
	echo "installing chrome..."
	#http://askubuntu.com/questions/79280/how-to-install-chrome-browser-properly-via-command-line
	
	sudo apt-get install libxss1 libappindicator1 libindicator7
	wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
	sudo dpkg -i google-chrome*.deb
	sudo apt-get install -fy
	sudo dpkg -i google-chrome*.deb
fi

echo "checking if jdk is installed..."
if [[ -f "/usr/bin/java" ]]; then
	echo "jdk already installed,skipping..."
else
	echo "installing jdk..."
	#sudo apt-get install openjdk-7-jdk -y
	
	sudo add-apt-repository -y ppa:openjdk-r/ppa
	sudo apt-get update
	sudo apt-get install openjdk-8-jdk -y
	#sudo update-alternatives --config java
	#sudo update-alternatives --config javac
	
fi

#
# Selenium Jar Link
# wget https://goo.gl/SP94ZB
# Running Selenium Server
# java -jar selenium*.jar
# INFO - Selenium Server is up and running on port 4444
#
# Chrome Driver
# http://chromedriver.storage.googleapis.com/2.33/chromedriver_linux64.zip
# extract chromedriver
# sudo mv chromedriver /usr/local/bin



#echo "checking if nodejs is installed..."
#if [[ -f "/usr/bin/nodejs" ]]; then
#	echo "nodejs already installed,skipping..."
#else
#	echo "installing nodejs..."
	
#	sudo apt-get install nodejs -y
#	sudo apt-get install npm -y
#	sudo ln -s /usr/bin/nodejs /usr/bin/node
#	sudo npm install -g bower -y
#	sudo npm install -g yo -y
#	sudo npm install -g npm

#	#npm install --global gulp-cli
#fi


#echo "checking if visual studio code is installed..."
#if [[ -f "/home/vagrant/.local/share/umake/web/visual-studio-code/bin/code" ]]; then
#	echo "visual studio code already installed,skipping..."
#else
#	echo "installing visual studio code..."
	#http://www.omgubuntu.co.uk/2015/05/how-to-install-microsoft-visual-studio-code-in-ubuntu
#	sudo add-apt-repository ppa:ubuntu-desktop/ubuntu-make -y
#	sudo apt-get update -y && sudo apt-get install ubuntu-make -y

#	umake web visual-studio-code --accept-license /home/vagrant/.local/share/umake/web/visual-studio-code
#fi


echo "checking dotnet core is installed..."
if [[ -f "/usr/bin/dotnet" ]]; then
	echo "dotnet core already installed,skipping..."
else
	echo "installing dotnet core ..."
	
	curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
	sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
	sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-trusty-prod trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
	sudo apt-get update
	sudo apt-get install dotnet-sdk-2.0.0 -y
	
	#You can opt out of telemetry by setting a DOTNET_CLI_TELEMETRY_OPTOUT environment variable to 1 using your favorite shell.
	#You can read more about .NET Core tools telemetry @ https://aka.ms/dotnet-cli-telemetry
	# .profile <-- append DOTNET_CLI_TELEMETRY_OPTOUT=1; export DOTNET_CLI_TELEMETRY_OPTOUT 
	#export DOTNET_CLI_TELEMETRY_OPTOUT=1
	#set DOTNET_CLI_TELEMETRY_OPTOUT=1
	#setx DOTNET_CLI_TELEMETRY_OPTOUT 1
fi

#https://github.com/foretagsplatsen/Divan
#https://github.com/rnewson/couchdb-lucene/tree/v0.4


if [[ -f "/usr/bin/docker" ]]; then
	echo "docker already installed,skipping..."
else
	echo "installing docker..."
	
	sudo apt-get update

	sudo apt-get install -y \
    linux-image-extra-$(uname -r) \
    linux-image-extra-virtual
	
	sudo apt-get update
	
	sudo apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    software-properties-common
	
	curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
	
	sudo add-apt-repository -y \
   "deb [arch=amd64] https://download.docker.com/linux/ubuntu \
   $(lsb_release -cs) \
   stable"
   
   sudo apt-get update
   
   sudo apt-get install -y docker-ce
   
fi

# echo "checking if webstorm is installed..."
# if [[ -f "/home/vagrant/webstorm/bin/WebStorm.sh" ]]; then
	# echo "webstorm already installed,skipping..."
# else
	# echo "installing webstorm..."
	
	# https://www.jetbrains.com/webstorm/download/download-thanks.html?platform=linux
	# wget https://download.jetbrains.com/webstorm/WebStorm-2016.1.2.tar.gz
	# sudo tar xfz WebStorm-*.tar.gz
	# sudo mkdir /home/vagrant/webstorm
	# sudo mv WebStorm-145.971.23/* /home/vagrant/webstorm
#	
# sed -e 's/<%=version>/^C017.06.15/g' index.html
#mdb-export /vagrant/source-code/scratch/owin/mmrds-importer/mapping-file-set/Maternal_Mortality.mdb AutopsyReport > autopsyreport.csv
#sudo apt-get install -y mdbtools	
# https://downloads.sourceforge.net/project/mdbtools/mdbtools/0.6pre1/mdbtools-0.6pre1.tar.gz?r=&ts=1493994048&use_mirror=superb-sea2
# /home/vagrant/Downloads/mdbtools-0.6pre1/
#   sudo apt-get install -y bison flex unixODBC libglib2.0-dev
# sudo apt-get install libglib2.0-dev

	
# fi

echo ""
echo ""
echo "*****************************"
echo "... provisioning finished ..."
echo "*****************************"
