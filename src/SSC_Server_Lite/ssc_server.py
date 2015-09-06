#!/usr/bin/python
# -*- coding:utf8 -*-

import socket
from base64 import b64encode, b64decode
from threading import Thread
from time import time, localtime, strftime
from Crypto.Cipher import AES  # You need "easy_install pyCrypto"


# ################Options BEGIN##################

# Settings
ip = "0.0.0.0"  # Listen IP

port = 12344  # listen port

key = "SSCv2*Default_AES@Key&1234567890"  # AES Key

buffersize = 102400  # Buffer Size

maxqueue = 1024  # Max Listen queue

BroadcastJoinLeave = True

ServerMsgName = "Server"

# Advance (if you don't know what's these things below,just don't change it)

mode = AES.MODE_ECB

Cryptor = AES.new(key, mode)

# #################Options END###################

clientDic = {}

BS = AES.block_size


def pad(s):
    return lambda s: s + (BS - len(s) % BS) * chr(BS - len(s) % BS)


def unpad(s):
    return lambda s: s[0:-ord(s[-1])]


def main():
    print """
 ____ ____   ____     ____
/ ___/ ___| / ___|   / ___|  ___ _ ____   _____ _ __
\___ \___ \| |   ____\___ \ / _ \ '__\ \ / / _ \ '__|
 ___) |__) | |__|_____|__) |  __/ |   \ V /  __/ |
|____/____/ \____|   |____/ \___|_|    \_/ \___|_|
==================================================================
Simple Secure Chat Server
Python Edition
==================================================================
"""

    MainSocketInit()
    AcceptHandler()


def Shutdown():
    S.close()
    exit()


def sysLog(msg):
    print "[%s] [SYS]: %s" % (strftime('%Y-%m-%d %H:%M:%S %Z', localtime(time())), msg)


def errLog(msg):
    print "[%s] [ERR]: %s" % (strftime('%Y-%m-%d %H:%M:%S %Z', localtime(time())), msg)


def msgLog(msg):
    print "[%s] [MSG]: %s" % (strftime('%Y-%m-%d %H:%M:%S %Z', localtime(time())), msg)


def Encrypt(data):
    return b64encode(Cryptor.encrypt(pad(data)))


def Decrypt(data):
    return unpad(Cryptor.decrypt(b64decode(data)))


def parseData(rawdata):
    first = Decrypt(rawdata)
    tmpsplit = first.split("|")
    result = []
    result.append(tmpsplit[0])
    result.append(Decrypt(tmpsplit[1]))
    return result


def parseMsg(cmd, msg):
    return Encrypt("%s|%s" % (cmd, Encrypt(msg))) + "\r\n"


def regClient(name, s):
    clientDic[name] = s
    pass


def unregClient(name):
    clientDic.pop(name)
    pass


def ClientNickNameExisted(name):
    if name in clientDic:
        return True
    else:
        return False


def Broadcast(msg, forall, exceptSocket=None):
    if forall:
        for key in clientDic:
            clientDic[key].send(msg)
            pass
    else:
        for key in clientDic:
            if clientDic[key] != exceptSocket:
                clientDic[key].send(msg)


def MainSocketInit():
    sysLog("Server is starting...")
    try:
        global S
        S = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        S.bind((ip, port))
        S.listen(maxqueue)
    except Exception, e:
        errLog("Maybe listen port used")
        Shutdown()


def AcceptHandler():
    sysLog("Server now listening on: %s:%s" % (ip, port))

    try:
        while True:
            clientSocket, addr = S.accept()

            sysLog("%s - Connected" % str(addr))

            t = Thread(target=ClientHandler, args=(clientSocket, addr))
            t.start()
    except KeyboardInterrupt:
        print "\r\n"
        sysLog("User Stop Server (KeyboardInterrupt)")
        Shutdown()
    except Exception, e:
        errLog("Unknown Exception")
        Shutdown()


def ClientHandler(client, addr):
    client.settimeout(15)
    nicknameSeted = False
    nickname = ""

    try:
        while True:
            recvdata = client.recv(buffersize)
            if len(recvdata) > 0:
                data = parseData(recvdata)

                # Nick command
                if data[0] == "NICK":
                    if nicknameSeted:
                        client.send(parseMsg("INFO", "ALREADY_SET"))
                        sysLog("%s - Set nickname - %s - %s" %
                               (str(addr), data[1], "Failed(ALREADY_SET)"))
                        continue
                    if ClientNickNameExisted(data[1]):
                        client.send(parseMsg("INFO", "NICKNAME_EXIST"))
                        sysLog("%s - Set nickname - %s - %s" %
                               (str(addr), data[1], "Failed(NICKNAME_EXIST)"))
                        continue
                    if (" " in data[1]) or (":" in data[1]) or (data[1][0] == "@") or (data[1][0] == "&"):
                        client.send(parseMsg("INFO", "NICKNAME_NOT_ALLOW"))
                        sysLog("%s - Set nickname - %s - %s" %
                               (str(addr), data[1], "Failed(NICKNAME_NOT_ALLOW)"))
                        continue

                    # All green! and set nickname now
                    regClient(data[1], client)
                    nickname = data[1]
                    nicknameSeted = True

                    client.settimeout(None)

                    client.send(parseMsg("INFO", "OK"))
                    sysLog("%s - Set nickname - %s - %s" % (str(addr), data[1], "OK"))

                    if BroadcastJoinLeave:
                        Broadcast(parseMsg("MSG", "@%s:%s" %
                                           (ServerMsgName, '"' + nickname + '" Joined.')), True)

                    continue

                # Msg command
                if data[0] == "MSG":
                    if not nicknameSeted:
                        client.send(parseMsg("INFO", "NICKNAME_NOT_SET"))
                        sysLog("%s - Send Msg - %s - %s" %
                               (str(addr), data[1], "Failed(NICKNAME_NOT_SET)"))
                        continue

                    # All green, broadcast msg

                    msgLog("%s (%s): %s" % (str(addr), nickname, data[1]))

                    Broadcast(parseMsg("MSG", "%s:%s" % (nickname, data[1])), False, client)
            else:
                # lost connection
                if nicknameSeted:
                    unregClient(nickname)
                client.close()
                if BroadcastJoinLeave and nicknameSeted:
                    Broadcast(parseMsg("MSG", "@%s:%s" %
                                       (ServerMsgName, '"' + nickname + '" Leaved.')), True)
                sysLog("%s (%s) - Disconnected" % (str(addr), nickname))
    except Exception, e:
        # lost connection
        if nicknameSeted:
            unregClient(nickname)

        client.close()

        if BroadcastJoinLeave and nicknameSeted:
            Broadcast(parseMsg("MSG", "@%s:%s" %
                               (ServerMsgName, '"' + nickname + '" Leaved.')), True)
        sysLog("%s (%s) - Disconnected")


if __name__ == '__main__':
    main()
