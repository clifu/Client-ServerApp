using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ClientHttp
{
    class ClientHttp
    {
        static void Main(string[] args)
        {
            string ipserver = "127.0.0.1";
            string sportserver = "8000";
            int portserver = 8000;
            string clientrequest;
            string tempstring;
            try
            {
                Console.WriteLine("Witaj drogi użytkowniku! W celu nawiązania połączenia podaj IP serwera oraz port!");
                Console.WriteLine("IP:");
                ipserver = Console.ReadLine();
                Console.WriteLine("Port:");
                sportserver = Console.ReadLine();
                portserver = Int32.Parse(sportserver);
                string x = "0";
                while (!x.Equals("3"))
                {
                    Console.WriteLine("Jaki jest twój następny krok drogi użytowkniku?");
                    Console.WriteLine("Naciśnij 1, aby stworzyć nową tabelę.");
                    Console.WriteLine("Naciśnij 2, aby stworzyć nowy wpis.");
                    Console.WriteLine("Naciśnij 3, aby rozłączyć sie z serwerem.");

                    x = Console.ReadLine();

                    if (x.Equals("1"))
                    {
                        Console.WriteLine("Wybrano tworzenie nowej tabeli!");
                        clientrequest = "1#";
                        Console.WriteLine("Podaj nazwe tabeli:");
                        tempstring = Console.ReadLine();
                        Console.WriteLine("\n");
                        clientrequest = clientrequest + tempstring;
                        while (tempstring != "esc" && tempstring != "Esc" && tempstring != "ESC")
                        {
                            Console.WriteLine("Podaj nazwę i wartość kolumny w formacie NAZWA TYP lub ESC, aby zakończyć: \n");
                            tempstring = Console.ReadLine();
                            if (tempstring != "esc" && tempstring != "Esc" && tempstring != "ESC")
                            {
                                clientrequest = clientrequest + "#" + tempstring;
                            }
                        }
                        Console.WriteLine(clientrequest);
                        metodapost(clientrequest, ipserver, sportserver);
                        clientrequest = null;
                        tempstring = null;
                        x = "0";
                    }
                    else if (x.Equals("2"))
                    {
                        clientrequest = "2#";
                        Console.WriteLine("Podaj nazwe tabeli:");
                        tempstring = Console.ReadLine();
                        Console.WriteLine("\n");
                        clientrequest = clientrequest + tempstring;
                        while (tempstring != "esc" && tempstring != "Esc" && tempstring != "ESC")
                        {
                            Console.WriteLine("Podaj wartość kolumny w formacie WARTOŚĆ (uważaj na typy zmiennych) lub ESC, aby zakończyć: \n");
                            tempstring = Console.ReadLine();
                            if (tempstring != "esc" && tempstring != "Esc" && tempstring != "ESC")
                            {
                                clientrequest = clientrequest + "#" + tempstring;
                            }
                        }

                        Console.WriteLine(clientrequest);
                        metodapost(clientrequest, ipserver, sportserver);

                        clientrequest = null;
                        tempstring = null;
                        x = "0";
                    }
                    else if (x.Equals("3"))
                    {
                        Console.WriteLine("Trwa zamykanie klienta...");
                        Console.WriteLine("Naciśnij dowolny przycisk!");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Wprowadzono niewłaściwy klawisz, spróbuj ponownie!");
                        x = "0";
                    }
                }


            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());

            }
            Console.ReadLine();
        }


        //Źródło: msdn
        public static async void metodapost(string clientrequest, string ip, string sportserver)
        {

            // Stworzenie nowego obiektu klienta
            HttpClient client = new HttpClient();
            // Zawiera elemnty nagłówka http
            HttpContent content = new StringContent(clientrequest);
            try
            {
                //Odpowiedzialne za wysłanie zawartosci 'conent' na odpowiedni adres
                HttpResponseMessage response = await client.PostAsync("http://" + ip + ":" + sportserver + "/", content);
                //Upewnienie odnośnie wysłania contentu na właściwy ares.
                response.EnsureSuccessStatusCode();
                // Oczeykiwanie na odpowiedz serwera.
                string mycontent = await response.Content.ReadAsStringAsync();

                Console.WriteLine(mycontent);
                Console.ReadKey();

                await response.Content.ReadAsStringAsync();
                client.Dispose();
            }
            catch (HttpRequestException err)
            {
                Console.WriteLine(err.ToString());
                client.Dispose();

            }
        }
    }
}