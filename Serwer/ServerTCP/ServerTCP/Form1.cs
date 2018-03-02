using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Net;
using System.Data.SqlClient;
using System.Text;
using System.IO;

namespace ServerTCP
{

    //Głównym źródłem był strony takie jak:
    //https://msdn.microsoft.com/pl-pl/library/mt472912(v=vs.110).aspx
    //https://stackoverflow.com/questions/tagged/c%23
    //https://youtube.com
    //książki o C# i bazach danych.
    public partial class Form1 : Form
    {
        //Obiekt klasy SqlConnection posłuży do obsługi połączenia pomiędzy serwerem, a bazą danych.
        SqlConnection mycon;

        //Obiekt klasy BackgroundWorker służy do uruchomienia operacji w oddzielnym wyspecjalizowanym wątku - tak, żeby nie blokować całego programu.
        //W przypadku tego programu służy do obsługi wątku zwiazanego z nasłuchiwaniem nadchodzących żądań HTTP.
        BackgroundWorker background_multiclients;

        //Obiekt klasy SqlDataAdapter służy jako połączenie miedzy bazą danych, a widokiem, w naszym przypadku, w DataGridView - odpowiedzialny za funkcje fill, update.
        SqlDataAdapter data_adapter;

        //Obiekt klasy DataTable obejmuje DataSet oraz DataView - odpowiedzialny za tworzenie baz danych, widoku itd.
        DataTable data_table;

        //Obiekt klasy SqlCommandBuilder służy do stworzenia komendy, a raczej SQL query, które zostanie później przesłane do bazy danych przez SqlDataAdapter.
        SqlCommandBuilder command_builder;

        //Obiekt klasy HttpListener będzie odpowiedzialny za nasłuchiwanie na danym IP i porcie nadchodzących żądań. 
        HttpListener listener;

        //Obiekt klasy HttpListenerRequest zawiera w sobie informacje o żądaniu takie jak metoda (get/post) żądania, User-agent (informacje o nazwie i wersji oprogramowania klienta) itd.
        HttpListenerRequest request;

        //Obiekt klasy StringBuidler służy do tworzenia zawartosci zmiennych typu string - zawiera szereg funkcji, które to ułatwiają.
        StringBuilder sb;

        //Obiekt klasy StreamReader służy do oczytu linii ze standardowego pliku tekstowego. 
        StreamReader sr;

        //Obiekt klasy SqlCommand służy do stworzenia komendy, która przekazana do SqlCommandBuildera przez SqlDataAdapter, zostanie następnie wykonana.
        SqlCommand control_manager_dialog;

        //Obiekt klasy HttpListenerContext posłuży do odbioru żądania klienta - zawiera w sobie Request i Response.
        HttpListenerContext context;

        //Obiekt klasy HttpListenerResponse posłuży do wysłania odpowiedzi do klienta.
        HttpListenerResponse response;

        //Zmienna służąca do odczytu SqlStringConnection z pliku.
        string line;

        //Nazwa tablicy z bazy danych.
        string databasename;

        //IP klienta, pobierane w momencie połączenia.
        string clientip = null;

        //Zmienna służąca do zapisu SqlQuery zwiazanej z tworzeniem nowych tablic.
        string sqlqueryformakingtable;

        //Zmienna służąca do zapisu SQLQuery zwiazanej z filtrowaniem tablicy.
        string sqlqueryforfilteringtable;

        //Zmienna służąca do zapisu SQLQuery zwiazanej z dodawaniem nowych warunków filtrowania.
        string tempsqlqueryforfilteringtable = null;

        //Zmienna łącząca ze sobą IP oraz port na którym będzie odbywało się nasłuchiwanie w celu przesłania jej do HttpListenera.
        string prefix;

        //Zmienna odpowiedzialna za przechowywanie IP na którym bedzie nasłuchiwała.
        string ipserver;

        //Zmienna odpowiedzialna za przechowwywanie portu na którym bedzie odbywało się nasłuchiwanie.
        string sportserver;

        //Zmienna przechowująca informacje na temat  połaczenia z bazą danych.
        string sqlconnectionstring;

        public Form1()
        {
            InitializeComponent();
            _Form1 = this;

            //Odczytanie danych do połączenia z bazą danych i przekazanie jej do zmiennej sqlconnectionstring.
            try
            {
                sb = new StringBuilder();
                using (sr = new StreamReader("Config.txt"))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        sb.AppendLine(line);
                    }
                }
                sqlconnectionstring = sb.ToString();
            }
            catch (Exception err)
            {
                textBox3.AppendText(err.ToString() + "\r\n");
            }

            //Stworzenie nowego połaczenia (!=połaczenie) niezbędnego do połaczenia z bazą danych.
            mycon = new SqlConnection();
            mycon.ConnectionString = @sqlconnectionstring;

            //Stworzenie wątku na którym będzie odbywało się nasłuchiwanie
            background_multiclients = new BackgroundWorker();
            background_multiclients.DoWork += server_fucntion;
            background_multiclients.WorkerReportsProgress = true;

        }

        public static Form1 _Form1;

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        //Odpowiedzialny za uruchomienie nasłuchiwania.
        private void button1_Click(object sender, EventArgs e)
        {

            try
            {

                ipserver = textBox1.Text;
                sportserver = textBox2.Text;
                updatetextbox3("Serwer zaczyna nasłuchiwanie na IP:" + ipserver + " PORT:" + sportserver);
                //Uruchomienie nasłuchiwania
                background_multiclients.RunWorkerAsync();

            }
            catch (Exception err)
            {
                textBox3.AppendText(err.ToString() + "\r\n");
            }
        }

        //Odpowiedzialny za połaczenie z bazą danych.
        private void button2_Click(object sender, EventArgs e)
        {
            //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
            try
            {
                if (mycon.State == System.Data.ConnectionState.Open)
                {

                    textBox3.AppendText("Połączenie z bazą danych już istnieje!" + "\r\n");
                }
                else
                {
                    mycon.Open();
                    if (mycon.State == System.Data.ConnectionState.Open)
                    {
                        textBox3.AppendText("Połączenie z bazą danych udane!" + "\r\n");
                    }
                    else
                    {
                        textBox3.AppendText("Połączenie z bazą danych nieudane!" + "\r\n");
                    }
                }
            }
            catch (Exception err)
            {
                textBox3.AppendText(err.ToString() + "\r\n");
            }
        }

        //Odpowiedzialny za wyświetlenie zawartości tablicy poziomu widoku Viewera.
        private void button3_Click(object sender, EventArgs e)
        {

            try
            {
                //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
                if (mycon.State != System.Data.ConnectionState.Open)
                {
                    mycon.Open();
                    if (mycon.State == System.Data.ConnectionState.Open)
                    {
                        textBox3.AppendText("Połączenie z bazą danych udane!" + "\r\n");
                    }
                    else
                    {
                        textBox3.AppendText("Połączenie z bazą danych nieudane!" + "\r\n");
                    }
                }
                //Możliwość edycji tabeli prosto z widoku z Viewerze.
                dataGridView1.ReadOnly = false;
                databasename = textBox4.Text;
                //Stworzenie SQLQuery, która zostanie później wysłana do bazy danych za pośrednictwem SqlCommandBuildera.                
                control_manager_dialog = new SqlCommand("SELECT * FROM " + databasename, mycon);
                //Stworzenie mostka między bazą danych a serwerem. 
                data_adapter = new SqlDataAdapter(control_manager_dialog);
                //Stworzenie SqlCommandBuildera, którt wysyła odpowiednie żądania do bazy danych w momencie uruchomienia DataAdaptera (działa jako słuchacz).
                command_builder = new SqlCommandBuilder(data_adapter);
                //Stworzenie DataTable (tablicy) do kótrej będą wpisywane zawartości tablic z bazy danych.
                data_table = new DataTable();
                //Wypełnienie DataTable za pomocą adaptera, mostka z bazą danych.
                data_adapter.Fill(data_table);
                //Dodanie źródła jako DataTable, pozwala na wyświetlenie tablicy w programie Viewer.
                dataGridView1.DataSource = data_table;
            }
            catch (Exception err)
            {
                textBox3.AppendText(err.ToString() + "\r\n");
            }

        }
        //Odpowiada za update tabeli w bazie danych.
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
                if (mycon.State != System.Data.ConnectionState.Open)
                {
                    mycon.Open();
                    if (mycon.State == System.Data.ConnectionState.Open)
                    {
                        textBox3.AppendText("Połączenie z bazą danych udane!" + "\r\n");
                    }
                    else
                    {
                        textBox3.AppendText("Połączenie z bazą danych nieudane!" + "\r\n");
                    }
                }
                databasename = textBox4.Text;
                //Stworzenie mostka między bazą danych a serwerem. 
                data_adapter = new SqlDataAdapter("SELECT * FROM " + databasename, _Form1.mycon);
                //Stworzenie SqlCommandBuildera, którt wysyła odpowiednie żądania do bazy danych w momencie uruchomienia DataAdaptera (działa jako słuchacz).
                command_builder = new SqlCommandBuilder(data_adapter);
                //Powoduje zaktualizowanie tablicy w bazie danych.
                data_adapter.Update(data_table);
            }
            catch (Exception err)
            {
                textBox3.AppendText(err.ToString() + "\r\n");
            }
        }

        //Odpowiedzialne za funkcje filtrowania
        private void button4_Click(object sender, EventArgs e)
        {
            //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
            if (mycon.State != System.Data.ConnectionState.Open)
            {
                mycon.Open();
                if (mycon.State == System.Data.ConnectionState.Open)
                {
                    textBox3.AppendText("Połączenie z bazą danych udane!" + "\r\n");
                }
                else
                {
                    textBox3.AppendText("Połączenie z bazą danych nieudane!" + "\r\n");
                }
            }
            //Zmienna przechowująca znak zaznaczony podczas filtrowania.
            string radio = ReturnCheckedRadio(groupBox2);
            databasename = textBox4.Text;
            _Form1.sqlqueryforfilteringtable = "SELECT * FROM " + databasename + " WHERE ";
            if (_Form1.tempsqlqueryforfilteringtable == null)
            {
                _Form1.tempsqlqueryforfilteringtable = _Form1.sqlqueryforfilteringtable + textBox5.Text + radio + "'" + textBox6.Text + "'";
            }
            else
            {
                _Form1.tempsqlqueryforfilteringtable = _Form1.tempsqlqueryforfilteringtable + " OR " + textBox5.Text + radio + "'" + textBox6.Text + "'";
            }
            updatetextbox3("SQL QUERY wysłane do bazy danych: " + _Form1.tempsqlqueryforfilteringtable + "\r\n");
            //Stworzenie mostka między bazą danych a serwerem. 
            data_adapter = new SqlDataAdapter(_Form1.tempsqlqueryforfilteringtable, _Form1.mycon);
            //Stworzenie SqlCommandBuildera, którt wysyła odpowiednie żądania do bazy danych w momencie uruchomienia DataAdaptera (działa jako słuchacz).
            command_builder = new SqlCommandBuilder(data_adapter);
            //Stworzenie DataTable (tablicy) do kótrej będą wpisywane zawartości tablic z bazy danych.
            data_table = new DataTable();
            //Wypełnienie DataTable za pomocą adaptera, mostka z bazą danych.
            data_adapter.Fill(data_table);
            //Dodanie źródła jako DataTable, pozwala na wyświetlenie tablicy w programie Viewer.
            dataGridView1.DataSource = data_table;
        }

        //Odpowiedzialne za wyczyszczenie filtrowania i powrotu do standardowego widoku. 
        private void button6_Click_1(object sender, EventArgs e)
        {
            try
            {
                //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
                if (mycon.State != System.Data.ConnectionState.Open)
                {
                    mycon.Open();
                    if (mycon.State == System.Data.ConnectionState.Open)
                    {
                        textBox3.AppendText("Połączenie z bazą danych udane!" + "\r\n");
                    }
                    else
                    {
                        textBox3.AppendText("Połączenie z bazą danych nieudane!" + "\r\n");
                    }
                }
                databasename = textBox4.Text;
                _Form1.sqlqueryforfilteringtable = "SELECT * FROM " + databasename;
                _Form1.tempsqlqueryforfilteringtable = null;
                //Stworzenie mostka między bazą danych a serwerem. 
                data_adapter = new SqlDataAdapter(_Form1.sqlqueryforfilteringtable, _Form1.mycon);
                //Stworzenie SqlCommandBuildera, którt wysyła odpowiednie żądania do bazy danych w momencie uruchomienia DataAdaptera (działa jako słuchacz).
                command_builder = new SqlCommandBuilder(data_adapter);
                //Stworzenie DataTable (tablicy) do kótrej będą wpisywane zawartości tablic z bazy danych.
                data_table = new DataTable();
                //Wypełnienie DataTable za pomocą adaptera, mostka z bazą danych.
                data_adapter.Fill(data_table);
                //Dodanie źródła jako DataTable, pozwala na wyświetlenie tablicy w programie Viewer.
                dataGridView1.DataSource = data_table;
            }
            catch (Exception err)
            {
                updatetextbox3(err.ToString());
            }
        }

        //Funkcja dołaczona do BackGroundWorkera odpowiedzialna za działanie nasłuchiwania nadchodzących żądań.
        private void server_fucntion(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    prefix = "http://" + ipserver + ":" + sportserver + "/";
                    Listener(prefix);
                }
                catch (Exception err)
                {
                    updatetextbox3(err.ToString());
                }

            }
        }

        //Funkcja odpowiedzialna za uruchomienie nasłuchiwania żądań.

        //źródło: https://msdn.microsoft.com/pl-pl/library/system.net.httplistener(v=vs.110).aspx
        public static void Listener(string prefix)
        {
            try
            {
                if (!HttpListener.IsSupported)
                {
                    updatetextbox3("Aby używać klasy HttpListener wymagany jes Windows XP SP2 lub Server 2003." + "\r\n");
                    return;
                }

                if (prefix == null || prefix.Length == 0)
                {
                    throw new ArgumentException("prefix");
                }

                //Stworzenie HttpListenera.
                _Form1.listener = new HttpListener();
                //Dodanie IP i portu na którym ma nasłuchiwać.
                _Form1.listener.Prefixes.Add(prefix);
                //Uruchomienie HttpListenera.
                _Form1.listener.Start();
                updatetextbox3("Nasłuchiwanie..." + "\r\n");
                //Dodanie do HttpListenera metody GetContext() powoduje rozpoczęcie oczekiwanie na nowego klienta.
                _Form1.context = _Form1.listener.GetContext();
                //Gdy nadejdzie połaczenie do HttoContext przekazane zostanie żądanie.
                _Form1.request = _Form1.context.Request;
                //Uruchomienie funkcji odpowiedzialnych za obsługę danych otrzymanych w żądaniu.
                RequestData(_Form1.request);
                //Otrzymanie odpowiedzi na żądanie klienta.
                _Form1.response = _Form1.context.Response;
                //Stwórzy odpowiedź dla klienta.               
                string responseString = "200 OK";
                //Zakodowanie odpowiedzi dla klienta w tablicy znaków.
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                _Form1.response.ContentLength64 = buffer.Length;
                //Przekazanie odpowiedzi na wyjście - "połączenie" z klientem.
                System.IO.Stream output = _Form1.response.OutputStream;
                //Wysłanie odpowiedzi do klienta.
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                _Form1.listener.Stop();
            }
            catch (Exception err)
            {
                updatetextbox3(err.ToString());
                _Form1.listener.Stop();
            }
        }

        //Funkcja odpowiedzialna za odpowiednią obróbkę otrzymanego żądania.

        //źródło: https://msdn.microsoft.com/en-us/library/system.net.httplistenerrequest.contentlength64(v=vs.110).aspx
        public static void RequestData(HttpListenerRequest request)
        {

            try
            {
                //Odczytanie IP klienta.
                _Form1.clientip = request.UserHostAddress;

                //Sprawdzenie czy żądanie posiada zawartość.
                if (!request.HasEntityBody)
                {
                    updatetextbox3("W żądaniu nie było żadnych danych." + "\r\n");
                    return;
                }
                //Stworzenie ciała żądania do którego zostana przekazane dane otrzymane od klienta.
                System.IO.Stream body = request.InputStream;
                //Sposób kodowania.
                System.Text.Encoding encoding = request.ContentEncoding;
                //Odkodowanie zawartości.
                System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
                if (request.ContentType != null)
                {
                    updatetextbox3("Typ zawartości żądania klienckiego: " + request.ContentType + "\r\n");
                }
                updatetextbox3("Początek zawartości żądania: " + "\r\n");

                //Zmienna przechowująca całą zawartość otrzymanej zawartości.
                string s = reader.ReadToEnd();

                //Wywołanie funkcji odpowiedzialnej za uaktualnienie bazy danych.
                uaktualnieniebazy(s);
                body.Close();
                reader.Close();
            }
            catch (Exception err)
            {
                updatetextbox3(err.ToString());
                _Form1.listener.Stop();
            }
        }

        //Funkcja zajmująca się uaktualnieniem bazy danych zgodnie z zawartością żądania klienta.
        public static void uaktualnieniebazy(string clientrequest)
        {
            //Zmienne pomocnicze potrzebne do obsługi bazy danych.
            SqlDataReader reader;
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = _Form1.mycon;
            cmd.CommandType = CommandType.Text;
            //Odczytanie daty nadajeśćia połączenia - zostanie dodane do tablicy w bazie danych.
            DateTime localDate = DateTime.Now;

            try
            {
                //Sprawdzenie czy istnieje już połączenie z bazą danych, jeśli nie to następuje inicjacja połaczenia.
                if (_Form1.mycon.State != System.Data.ConnectionState.Open)
                {
                    _Form1.mycon.Open();
                    if (_Form1.mycon.State == System.Data.ConnectionState.Open)
                    {
                        updatetextbox3("Połączenie z bazą danych udane!" + "\r\n");
                    }
                    else
                    {
                        updatetextbox3("Połączenie z bazą danych nieudane!" + "\r\n");
                    }
                }

                //Znaki oddzielające poszczególne części żądania klienta.
                char[] delimiterChars = { '#' };
                //Podzielenie żądania na tablicę stringów.
                string[] words = clientrequest.Split(delimiterChars);

                updatetextbox3(clientrequest + "\r\n");
                updatetextbox3("Koniec danych z żądania.");


                //Jeśli na początku żądania wystąpi '1' oznacza to, że klient w swoim żądaniu wystosował informacje o stworzeniu nowej tablicy, nowego typu zdarzeń.
                if (words[0] == "1")
                {
                    _Form1.sqlqueryformakingtable = "CREATE TABLE " + words[1] + "(DATE varchar(255), CLIENTIP varchar(255), ";
                    for (int i = 2; i < words.Length; i++)
                    {
                        if (i + 1 < words.Length)
                        {
                            _Form1.sqlqueryformakingtable = _Form1.sqlqueryformakingtable + words[i] + ",";
                        }
                        else
                        {
                            _Form1.sqlqueryformakingtable = _Form1.sqlqueryformakingtable + words[i] + ");";
                        }
                    }
                    updatetextbox3("SQL QUERY wysłane do bazy danych: " + _Form1.sqlqueryformakingtable + "\r\n");
                }
                //Jeśli na początku żądania wystąpi '2' oznacza to, że klient w swoim żądaniu wystosował informacje o stworzeniu nowego wpisu w tablicy już istniejącej.
                else if (words[0] == "2")
                {
                    _Form1.sqlqueryformakingtable = "INSERT INTO " + words[1] + " VALUES " + "(" + "'" + localDate + "'" + "," + "'" + _Form1.clientip + "'" + ",";
                    for (int i = 2; i < words.Length; i++)
                    {
                        if (i + 1 < words.Length)
                        {
                            _Form1.sqlqueryformakingtable = _Form1.sqlqueryformakingtable + "'" + words[i] + "'" + ",";
                        }
                        else
                        {
                            _Form1.sqlqueryformakingtable = _Form1.sqlqueryformakingtable + "'" + words[i] + "'" + ");";
                        }
                    }
                    updatetextbox3("SQL QUERY wysłane do bazy danych: " + _Form1.sqlqueryformakingtable + "\r\n");
                }

                cmd.CommandText = _Form1.sqlqueryformakingtable;
                //Wykonanie komendy - przesłanie jej do bazy danych.
                reader = cmd.ExecuteReader();
            }
            catch (Exception err)
            {
                updatetextbox3(err.ToString());
            }
        }

        //Funkcja zwracająca informacje odnośnie zawartości tekstowej wciśniętego RadioCheck'a.
        string ReturnCheckedRadio(Control container)
        {
            foreach (var control in container.Controls)
            {
                RadioButton radio = control as RadioButton;

                if (radio != null && radio.Checked)
                {
                    return radio.Text;
                }
            }

            return null;
        }

        //Funkcja umożliwiająca aktualizowanie pola "LOGI" (czyli textbox3) z innego wątku niż ten na którym jest on uruchomiony. 
        public static void updatetextbox3(string updateinfo)
        {
            //Tworzy nowy delegat do wątku który aktualnie posiada kontrolę nad danym elementem.
            _Form1.textBox3.Invoke(new Action(delegate ()
            {
                _Form1.textBox3.AppendText(updateinfo + "\r\n");
            }));
        }

    }

}
