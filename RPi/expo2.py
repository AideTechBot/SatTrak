import socket 
import sys
import packetlib
import os

HOST = ''
PORT = 3762
FREQUENCY = 100.0
MIN = 1.05
MAX = 2.05
MIN2 = 1.28
MAX2 = 1.83
msPerCycle = 1000.0 / FREQUENCY


def aim_at(azimuth, elevation):
	#fractions for az and el
	decaz = 1.0 - (float(azimuth) / 360.0)
	decel = float(elevation) / 90.0

	#pulse len for az and el
	azplen = MIN + (decaz * (MAX - MIN))
	elplen = MIN2 + (decel * (MAX2 - MIN2))
	print elplen

	dutycycle =  azplen / msPerCycle
	dutycycle2 = elplen / msPerCycle
	print dutycycle2
	if (float(elevation) < 0.0):
		print "ya"
		dutycycle2 = MIN2 / 10
		elplen = MIN2

	if(dutycycle < 0.105 or dutycycle > 0.205 or azplen < 1.05 or azplen > 2.05 or elplen < 1.05 or elplen > 2.05 or dutycycle2 < 0.105 or dutycycle2 > 0.205):
		raise RuntimeError("Invalid dutycycles/pulse lengths")

	os.system("echo \"4=" + str(dutycycle) + "\" > /dev/pi-blaster")
	os.system("echo \"17=" + str(dutycycle2) + "\" > /dev/pi-blaster")
	



s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
print "Socket creation"

try:
	s.bind((HOST, PORT))
except socket.error as msg:
	print "Bind failed. Error Code : " + str(msg[0]) + " Message " + msg[1]
	sys.exit()

print "Socket bind complete"

s.listen(10)
print "Socket listening on: " + str(PORT)

try:
	while 1:
		conn, addr = s.accept()
		print 'Connected with ' + addr[0] + ':' + str(addr[1])

		while True:
			try:
				data = conn.recv(1024)
			except socket.error as e:
				print "Unexpected shutdown of " + addr[0]
			if not data:
				print addr[0] + " has disconnected"
				break
			reply = "[" + addr[0] + "] " + data
			print reply
			coords = packetlib.parse_data(data, "|")
			aim_at(coords[0], coords[1])	
except KeyboardInterrupt as e:
	os.system("echo \"release 4\" > /dev/pi-blaster")
	os.system("echo \"release 17\" > /dev/pi-blaster")
	print "Quitting..."

s.close()
