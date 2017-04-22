using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BookLibrary
{
    public class BookDAO : IBook
    {
        private List<Book> bookList = new List<Book>();

        public int calcutePercentReadBook(int pageNumb, int currentPage)
        {
            double percentBookRead = 0;
            percentBookRead = (100 / Convert.ToDouble(pageNumb)) * Convert.ToDouble(currentPage);
            return Convert.ToInt32(percentBookRead);
        }

        public List<Book> getBookList()
        {
            return bookList;
        }

        public T readBooksFromFile<T>()
        {
            using (Stream stream = File.Open("UsersBooks.bin", FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        public void writeBooksToFile<T>(T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open("UsersBooks.bin", append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }
    }
}
