using System;
using System.Collections.Generic;
using System.Text;

namespace PDFCreator
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully. Welcome {_username}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
        }

        public static void MakeNewDocument(int _fromClient, Packet _packet)
        {
            string name = _packet.ReadString();
            int amountOfPages = _packet.ReadInt();
            float width = _packet.ReadFloat();
            float height = _packet.ReadFloat();

            PDFCreator.pdfCreator.CreateDocument(name, amountOfPages, width, height);
            Console.WriteLine($"{name} Page received! Amount of pages: {amountOfPages}, Width: {width}, Height: {height}");
        }

        public static void ImageReceived(int _fromClient, Packet _packet)
        {
            string documentName = _packet.ReadString();
            string path = _packet.ReadString();
            int pageNR = _packet.ReadInt();
            float pivotX = _packet.ReadFloat();
            float pivotY = _packet.ReadFloat();
            float sizeX = _packet.ReadFloat();
            float sizeY = _packet.ReadFloat();
            float posX = _packet.ReadFloat();
            float posY = _packet.ReadFloat();

            PDFCreator.pdfCreator.DrawImage(documentName, path, pageNR, pivotX, pivotY, sizeX, sizeY, posX, posY);
            Console.WriteLine($"Image received! Path: {path}, Width: {sizeX}, Height: {sizeY}, PositionX: {posX}, PositionY: {posY}");
        }

        public static void UTF8RTFTextReceived(int _fromClient, Packet _packet)
        {
            string documentName = _packet.ReadString();
            int textLength = _packet.ReadInt();
            string RTFtext = Encoding.UTF8.GetString(_packet.ReadBytes(textLength));
            int pageNR = _packet.ReadInt();
            string fontFamily = _packet.ReadString();
            float fontSize = _packet.ReadFloat();
            float pivotX = _packet.ReadFloat();
            float pivotY = _packet.ReadFloat();
            float sizeX = _packet.ReadFloat();
            float sizeY = _packet.ReadFloat();
            float posX = _packet.ReadFloat();
            float posY = _packet.ReadFloat();

            PDFCreator.pdfCreator.DrawString(documentName, RTFtext, pageNR, fontFamily, fontSize, pivotX, pivotY, sizeX, sizeY, posX, posY);
            Console.WriteLine($"RTFText received! text: {RTFtext}, Width: {sizeX}, Height: {sizeY}, PositionX: {posX}, PositionY: {posY}");
        }

        public static void HyperlinkReceived(int _fromClient, Packet _packet)
        {
        }

        public static void SaveDocument(int _fromClient, Packet _packet)
        {
            string name = _packet.ReadString();
            PDFCreator.pdfCreator.SaveDocument(name);
            Console.WriteLine("Document saved!");
        }
    }
}
