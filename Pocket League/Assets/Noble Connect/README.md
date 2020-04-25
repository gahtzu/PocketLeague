Website - https://noblewhale.com
Dashboard - https://noblewhale.com/dashboard
Docs - https://noblewhale.com/docs
FAQ - https://noblewhale.com/faq

# README
Adds relays and punchthrough to UNet.
Guarantees your players can connect while reducing latency and saving you money by connecting players directly whenever possible.
Your players won't need to worry about forwarding ports or fiddling with router settings, they can just sit back, relax, and enjoy your beautiful game.

Supports Windows, Linux, OSX, Android, and iOS.

*Note: Web builds are not supported.*

# How to Use
In order to use the Noble Connect relay and punchthrough services you will need to sign up for an account. You can do this on our 
website or through the Unity Engine at Window->Noble Connect->Setup. It is free to sign up but your CCU and bandwidth will be limited. 
In order to raise the limits you will either need to purchase the Starter Pack or one of the monthly plans.

## Step 1 - Set up
	1. You can access the setup window at any time by going to Window->Noble Connect->Setup.
	2. Enter your email address to sign up, or enter your Game ID if you already have an account.
		* You can get your Game ID any time from the dashboard at https://noblewhale.com/dashboard

## Step 2 - Test it
	1. Add the "Noble Connect/Examples/Network Manager/Network Manager Example.unity" scene to the build settings.
	2. Build for your desired platform and run the build.
	3. Click "Host" in the build.
		* Note the IP and port that are displayed, this is the address that clients will use to connect to the host.
	4. Run the Network Manager Example scene in the editor.
	5. Click "Client" and then enter the ip and port from the host.
	6. Click "Connect" to connect to the host.
		* When the connection is complete you will see the connection type displayed on the client.

# Examples
Check out the Example scenes and scripts to see common ways to get connected. Each example includes a README file with more detailed instructions.
If you need any more information don't hesitate to contact us at nobleconnect@noblewhale.com

# What next?
Most people should extend from the provided NobleNetworkManager.
If you prefer something a little lower level, you can also use the NobleServer and NobleClient classes directly to Listen() and Connect().
If you're using UNet then most things will work exactly the same as you are used to. 

**Note: For most methods that you override in NobleNetworkManager you will want to make sure to call the base method to avoid causing unexpected behaviour.**

# Differences from UNet
The main difference is that you will use the NobleNetworkManager instead of Unity's NetworkManager, or the NobleServer and NobleClient instead of Unity's NetworkServer and NetworkClient.

Another difference is that the host will not know the address that clients should connect to until it has been assigned by the Noble Connect servers. 
You will need to override the OnServerPrepared() method to know when this has happened and to get the hostAddress and hostPort (collectively known as the HostEndPoint) 
that clients should use to connect to the host. This is generally when you would create a match if you're using a matchmaking system. You can also get 
the HostEndPoint address any time after it has been assigned via NobleServer.HostEndPoint or NetworkManager.HostEndPoint.

# Regions
By default the closest region will be selected automatically. You can also manually select the region by passing a GeographicRegion at runtime.
You can see this in any of the example scenes.

We have servers in the following regions:
	* US_EAST - Virginia
	* US_WEST - California
	* EUROPE - Amsterdam
	* AUSTRALIA - Sydney
	* SOUTH_AMERICA - São Paulo
	* ASIA_PACIFIC - Singapore

# How it Works
Punchthrough and relays work according to the [ICE](https://tools.ietf.org/html/rfc5245), [TURN](https://tools.ietf.org/html/rfc5766), and [STUN](https://tools.ietf.org/html/rfc5389) specifications.