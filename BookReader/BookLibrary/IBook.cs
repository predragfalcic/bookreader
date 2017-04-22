using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BookLibrary
{
    interface IBook
    {
        void writeBooksToFile<T>(T objectToWrite, bool append = false);
        T readBooksFromFile<T>();
        List<Book> getBookList();
        int calcutePercentReadBook(int pageNumb, int currentPage);
    }
}
