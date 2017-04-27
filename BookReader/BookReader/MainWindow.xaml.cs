using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using BookLibrary;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.Threading;

namespace BookReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged

    {
        private int _bookReadProcent;

        public int BookReadProcent
        {
            get { return _bookReadProcent; }
            set
            {
                if (value != _bookReadProcent)
                {
                    _bookReadProcent = value;
                    OnPropertyChanged("BookReadProcent");
                }
            }
        }

        protected virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static RoutedCommand helpWindow = new RoutedCommand();

        FileStream stream;

        HelpWindow hw;

        private SolidColorBrush modeBgColorN;
        private SolidColorBrush modeBgColorD;
        private SolidColorBrush modeFgColorN;
        private SolidColorBrush modeFgColorD;
        private SolidColorBrush currBg;
        private SolidColorBrush currFg;

        private static String currentMode = "DMODE";

        private ObservableCollection<Book> bookList = new ObservableCollection<Book>();

        private BookDAO bookDAO = new BookDAO();

        private Book book;

        private double width = 0;
        private double height = 0;

        private FontDialog fd;
        private ColorDialog cd;


        public MainWindow()
        {
            InitializeComponent();

            helpWindow.InputGestures.Add(new KeyGesture(Key.F11, ModifierKeys.Control));

            CommandBindings.Add(new CommandBinding(helpWindow, Help_Click));

            fd = new FontDialog();
            cd = new ColorDialog();

            modeBgColorN = new SolidColorBrush(Color.FromArgb(255, 10, 0, 0));
            modeBgColorD = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            modeFgColorN = Brushes.NavajoWhite;
            modeFgColorD = Brushes.Black;

            //U zavisnosti od trenutnog moda podesavamo trenutnu boju teksta i pozadine
            if (currentMode == "DMODE")
            {
                currBg = modeBgColorD;
                currFg = modeFgColorD;
            }
            else
            {
                currBg = modeBgColorN;
                currFg = modeFgColorN;
            }

            //Podesavanje boje teksta i pozadine flowReader-a
            document.Background = currBg;
            document.Foreground = currFg;

            // Read books from file
            try
            {
                bookList = bookDAO.readBooksFromFile<ObservableCollection<Book>>();
            }
            catch (FileNotFoundException) { }

            listboxBooks.ItemsSource = bookList;
        }

        private void helpWindowKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                hw = new HelpWindow();
                hw.Show();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Files (*.txt)|*.txt";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {

                CloseAnnotations();

                //Ukoliko je neka knjiga otvorena onda sacuvaj izmene u fajl pre
                //nego sto otvoris novu
                if (book != null) {
                    saveBookData();
                }
                // Open document 
                string filename = dlg.FileName;


                ReadBook(filename);

                if (!checkIfBookExists(dlg.SafeFileName))
                {

                    if (bookList.Count() > 0)
                    {
                        book = new Book(bookList.Last().id + 1, filename, dlg.SafeFileName);
                    }
                    else
                    {
                        book = new Book(1, filename, dlg.SafeFileName);
                    }

                    if (WindowState == System.Windows.WindowState.Maximized)
                    {
                        book.isMaximizedWindow = true;
                    }
                    else if (book.isMaximizedWindow)
                    {
                        WindowState = System.Windows.WindowState.Maximized;
                        leftPanel.Visibility = System.Windows.Visibility.Collapsed;
                        rightPanel.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    timer.Start();
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        FlowDocReader.GoToPage(book.currentPageNum);
                        //System.Windows.MessageBox.Show(FlowDocReader.MasterPageNumber.ToString());
                    };
                    srediDocument();

                    bookList.Add(book);

                    

                    //Save the book to file
                    //bookDAO.writeBooksToFile<ObservableCollection<Book>>(bookList);
                }
                else
                {
                    book = findBookByName(dlg.SafeFileName);

                    System.Windows.Application.Current.MainWindow.Width = book.windowWidth;
                    System.Windows.Application.Current.MainWindow.Height = book.windowHeight;
                    if (book.isMaximizedWindow)
                    {
                        WindowState = System.Windows.WindowState.Maximized;
                        leftPanel.Visibility = System.Windows.Visibility.Collapsed;
                        rightPanel.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                    timer.Start();
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        FlowDocReader.GoToPage(book.currentPageNum);
                        //System.Windows.MessageBox.Show(FlowDocReader.MasterPageNumber.ToString());
                    };

                    //book.readBook = bookDAO.calcutePercentReadBook(FlowDocReader.PageCount, FlowDocReader.MasterPageNumber);
                    

                    srediDocument();
                    // Calculate the percent that the user read
                    //book.readBook = bookDAO.calcutePercentReadBook(book.numPages, book.currentPageNum);

                }
                LoadAnnotations(book);
            }
        }

        private void ReadBook(string filename){

            Paragraph paragraph = new Paragraph();
            string text = System.IO.File.ReadAllText(filename);
            paragraph.Inlines.Add(text);

            document = new FlowDocument(paragraph);
            // document.ColumnGap = 0;
            document.ColumnWidth = 2000;
            document.TextAlignment = TextAlignment.Center;
            document.Background = currBg;
            document.Foreground = currFg;

            FlowDocReader.Document = document;
            //FlowDocReader.Find();
        }


        private void LoadAnnotations(Book b) {
            // Enable and load annotations
            AnnotationService service = AnnotationService.GetService(FlowDocReader);
            if (service == null)
            {
                string fullBookName = book.name;
                string bookName = fullBookName.Remove(fullBookName.Length - 4);
                string annotationFileName = bookName + b.id.ToString() + ".xml";
                stream = new FileStream(annotationFileName, FileMode.OpenOrCreate);
                service = new AnnotationService(FlowDocReader);
                AnnotationStore store = new XmlStreamStore(stream);
                store.AutoFlush = true;
                service.Enable(store);
            }
        }

        private void CloseAnnotations(){
            AnnotationService service = AnnotationService.GetService(FlowDocReader);
            if (service != null && service.IsEnabled)
            {
                service.Disable();
                stream.Close();
            }
        }

        protected void OnInitialized(object sender, EventArgs e)
        {
            // Enable and load annotations
            if (book != null)
            {
                LoadAnnotations(book);
            }
        }

        protected void OnClosing(object sender, EventArgs e) 
        {
            saveBookData();
        }

        protected void OnClosed(object sender, EventArgs e)
        {
            CloseAnnotations();
        }

        private void saveBookData() {
            width = System.Windows.Application.Current.MainWindow.ActualWidth;
            height = System.Windows.Application.Current.MainWindow.ActualHeight;
            
            //Get the current page number if book is opened
            if (book != null)
            {
                int currentPageNumber = FlowDocReader.MasterPageNumber;
                book.currentPageNum = currentPageNumber;
                book.windowWidth = width;
                book.windowHeight = height;
                book.readBook = bookDAO.calcutePercentReadBook(FlowDocReader.PageCount, currentPageNumber);
                BookReadProcent = book.readBook;
            }
            bookDAO.writeBooksToFile<ObservableCollection<Book>>(bookList);
        }

        private void listboxBooks_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listboxBooks.SelectedItem != null)
            {
                saveBookData();

                CloseAnnotations();
                Book item = (Book)listboxBooks.SelectedItem;
                
                book = findBookByName(item.name);

                ReadBook(book.pathToBook);

                System.Windows.Application.Current.MainWindow.Width = book.windowWidth;
                System.Windows.Application.Current.MainWindow.Height = book.windowHeight;
                
                if (book.isMaximizedWindow) {
                    WindowState = System.Windows.WindowState.Maximized;
                    leftPanel.Visibility = System.Windows.Visibility.Collapsed;
                    rightPanel.Visibility = System.Windows.Visibility.Collapsed;
                }

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                timer.Start();
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    FlowDocReader.GoToPage(book.currentPageNum);
                    //System.Windows.MessageBox.Show(FlowDocReader.MasterPageNumber.ToString());
                };

                srediDocument();

                LoadAnnotations(book);

                

                //txt_book_pages.Text = book_pages[currentPageNumber - 1]; // Display the text from the page the user last saved
            }
        }

        //Find book with the given name
        private Book findBookByName(string name)
        {
            foreach (Book book in bookList)
            {
                if (book.name.Equals(name))
                {
                    return book;
                }
            }
            return null;
        }

        //Check if book with that id exists
        private bool checkIfBookExists(string bookName)
        {
            foreach (Book book in bookList)
            {
                if (book.name.Equals(bookName))
                {
                    return true;
                }
            }
            return false;
        }

        private void NormalScreenMode_Click(object sender, RoutedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Normal;
            if (book != null)
            {
                book.isMaximizedWindow = false;
            }
            rightPanel.Visibility = System.Windows.Visibility.Visible;
            leftPanel.Visibility = System.Windows.Visibility.Visible;
            System.Windows.Application.Current.MainWindow.Width = 900;
            System.Windows.Application.Current.MainWindow.Height = 600;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (book != null)
            {
                fd.ShowColor = true;
                DialogResult dr = fd.ShowDialog();
                if (dr != System.Windows.Forms.DialogResult.Cancel)
                {
                    document.FontFamily = new FontFamily(fd.Font.Name);
                    document.FontSize = fd.Font.Size * 96.0 / 72.0;
                    document.FontWeight = fd.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
                    document.FontStyle = fd.Font.Italic ? FontStyles.Italic : FontStyles.Normal;
                    Color color = (Color)ColorConverter.ConvertFromString(fd.Color.Name);
                    document.Foreground = new SolidColorBrush(color);

                    book.font = fd.Font.Name;
                    book.fontSize = fd.Font.Size;
                    book.fontWeight = fd.Font.Bold;
                    book.fontStyle = fd.Font.Italic;
                    book.foreground = fd.Color.Name;
                }
            }
            else {
                System.Windows.MessageBox.Show("Prvo otvorite knjigu");
            }
        }

        private void srediDocument() 
        {
            document.FontFamily = new FontFamily(book.font);
            document.FontSize = book.fontSize * 96.0 / 72.0;
            document.FontWeight = book.fontWeight ? FontWeights.Bold : FontWeights.Regular;
            document.FontStyle = book.fontStyle ? FontStyles.Italic : FontStyles.Normal;
            Color color = (Color)ColorConverter.ConvertFromString(book.foreground);
            document.Foreground = new SolidColorBrush(color);

            if (book.background.Contains(","))
            {
                char[] delimiters = { ',' };
                string[] boje = book.background.Split(delimiters);
                Color colorBackground = Color.FromArgb(byte.Parse(boje[0]), byte.Parse(boje[1]), byte.Parse(boje[2]), byte.Parse(boje[3]));
                document.Background = new SolidColorBrush(colorBackground);
            }
            else {
                document.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(book.background));
            }
            FlowDocReader.Find();
        }

        private void BackgroundColorPicker_Click(object sender, RoutedEventArgs e)
        {
            if (book != null)
            {
                cd.FullOpen = true;
                if (cd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
                {
                    try
                    {
                        document.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cd.Color.Name));
                        book.background = cd.Color.Name;
                    }
                    catch (FormatException)
                    {
                        Color color = Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B);
                        document.Background = new SolidColorBrush(color);
                        book.background = cd.Color.A.ToString() + "," + cd.Color.R.ToString() + "," + cd.Color.G.ToString() + "," + cd.Color.B.ToString();
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Prvo otvorite knjigu");
            }
        }

        private void day_night_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == "DMODE")
            {
                currentMode = "NMODE";
                currBg = modeBgColorN;
                currFg = modeFgColorN;
            }
            else
            {
                currentMode = "DMODE";
                currBg = modeBgColorD;
                currFg = modeFgColorD;
            }

            document.Background = currBg;
            document.Foreground = currFg;
        }

        private void FullScreenMode_Click(object sender, RoutedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Maximized;
            if (book != null)
            {
                book.isMaximizedWindow = true;
            }
            leftPanel.Visibility = System.Windows.Visibility.Collapsed;
            rightPanel.Visibility = System.Windows.Visibility.Collapsed;
            document.PagePadding = new Thickness(300);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            hw = new HelpWindow();
            hw.Show();
        }
        
        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listboxBooks.SelectedItem != null) {

                Book item = (Book)listboxBooks.SelectedItem;

                book = findBookByName(item.name);

                string fullBookName = book.name;
                string bookName = fullBookName.Remove(fullBookName.Length - 4);
                string annotationFileName = bookName + book.id.ToString() + ".xml";

                bookList.RemoveAt(listboxBooks.SelectedIndex);
                CloseAnnotations();
                File.Delete(annotationFileName);
                document.Blocks.Clear();
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                document.PageHeight = printDialog.PrintableAreaHeight;
                document.PageWidth = printDialog.PrintableAreaWidth;

                // Set margin to 1 inch
                document.PagePadding = new Thickness(96);

                // Get the FlowDocument's DocumentPaginator
                var paginatorSource = (IDocumentPaginatorSource)document;
                var paginator = paginatorSource.DocumentPaginator;

                // Print the Document
                printDialog.PrintDocument(paginator, "FS NoteMaster Document");
            }
        }
    }
}

