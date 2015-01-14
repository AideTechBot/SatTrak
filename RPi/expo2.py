import socket 
import sys

HOST = ''
PORT = 3762

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
		

s.close()
