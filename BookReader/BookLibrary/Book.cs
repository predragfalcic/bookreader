using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Media;

namespace BookLibrary
{
    [Serializable]
    public class Book
    {
        public int id { get; set; }
        public string pathToBook { get; set; }
        public string name { get; set; }
        public int currentPageNum { get; set; }
        public int readBook { get; set; }
        public bool active { get; set; }
        public double windowHeight { get; set; }
        public double windowWidth { get; set; }
        public bool isMaximizedWindow { get; set; }
        public string font { get; set; }
        public double fontSize { get; set; }
        public bool fontWeight { get; set; }
        public bool fontStyle { get; set; }
        public string foreground { get; set; }
        public string background { get; set; }

        public Book() { }

        public Book(int id, string pathToBook, string name)
        {
            this.id = id;
            this.pathToBook = pathToBook;
            this.name = name;
            this.currentPageNum = 1;
            this.readBook = 0;
            this.active = true;
            this.windowHeight = 800;
            this.windowWidth = 1200;
            this.isMaximizedWindow = false;
            this.font = "Ariel";
            this.fontSize = 12;
            this.fontWeight = false;
            this.fontStyle = false;
            this.foreground = "Black";
            this.background = "White";

        }

    }
}
