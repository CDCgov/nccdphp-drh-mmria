# -*- mode: ruby -*-
# vi: set ft=ruby :

Configuration_Version = 2

Vagrant.configure(Configuration_Version) do |config|
	# intall plugin to vbox guest additions updatedvagrant
	#vagrant plugin install vagrant-vbguest
	
	
	config.vm.define "devbox" do |devbox|
  
		devbox.vm.box = "ubuntu/trusty64"
	  
		#devbox.vm.network "forwarded_port", guest: 12345, host: 8085
		#devbox.vm.network "private_network", ip: "192.168.33.10"
	  
		#devbox.vm.synced_folder '.', '/vagrant', type: 'nfs'
	  
	  
		devbox.vm.provider "virtualbox" do |vb|
			# Display the VirtualBox GUI when booting the machine
			vb.gui = true
	  
			# Customize the amount of memory on the VM:
			vb.memory = (6 * 1024).to_s
			vb.cpus = 2
			vb.customize [
                        "modifyvm", :id,
			#"--paravirtprovider", "kvm", # for linux guest
			"--hwvirtex", "on",
			"--accelerate3d", "on",
                        "--monitorcount", "3",
                        "--vram", "256"
                      ]
			
		end

		# vbox_guest_additions_file = "%PROGRAMFILES%/Oracle/VirtualBox/VBoxGuestAdditions.iso"
		# if !(File.file?(vbox_guest_additions_file))
			# vbox_guest_additions_file = "C:/program-file-set/Oracle/VirtualBox/VBoxGuestAdditions.iso"
		# end

		# if !(File.file?(vbox_guest_additions_file))
			# vbox_guest_additions_file = "D:/programfileset/Oracle/VirtualBox/VBoxGuestAdditions.iso"
		# end

		# devbox.vbguest.iso_path = vbox_guest_additions_file
		devbox.vbguest.auto_update = true

		devbox.vm.provision "shell", path:  "provision-script-set/main.sh"
		
	  end
	  

end
