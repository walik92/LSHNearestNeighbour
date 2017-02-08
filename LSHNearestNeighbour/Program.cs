using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LSHNearestNeighbour
{
    internal class Program
    {
        /// <summary>
        ///     dowolna liczba pierwsza większa od m (max indeks piosenki)
        /// </summary>
        private static readonly int _p = 1018649;

        /// <summary>
        ///     liczba bandów
        /// </summary>
        private static readonly byte _b = 4;

        /// <summary>
        ///     liczba wierszy w bandzie
        /// </summary>
        private static readonly byte _r = 4;

        /// <summary>
        ///     Oszacuj podobieństwo Jaccarda
        /// </summary>
        /// <param name="sig1"></param>
        /// <param name="sig2"></param>
        /// <returns>Oszacowane Podobieństwo Jaccarda</returns>
        private static double ApproximateJaccard(int[] sig1, int[] sig2)
        {
            var n = 0;
            for (var i = 0; i < sig1.Length; i++)
                if (sig1[i] == sig2[i])
                    n++;
            return (double) n / sig1.Length;
        }

        /// <summary>
        ///     Oblicz faktyczne prawdopodobieństwo Jaccarda
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns>Faktyczne prawdopodobieństwo Jaccarda</returns>
        private static double CalculateJaccard(int[] set1, int[] set2)
        {
            var intersect = set1.Intersect(set2).Count();
            var sum = set1.Length + set2.Length - intersect;

            return (double) intersect / sum;
        }

        /// <summary>
        ///     Zwraca pojedynczą wartość funkcji haszującej
        /// </summary>
        /// <returns></returns>
        private static int Hash(int x, int m, int a, int b)
        {
            return (int) (((long) a * x + b) % _p) % m;
        }

        /// <summary>
        ///     Pobierz funkcję haszująca
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static List<int> GetHashFunctions(int m)
        {
            var result = new List<int>();
            var random = new Random();

            var a = random.Next(1, _p - 1);
            var b = random.Next(0, _p - 1);

            for (var i = 0; i < m; i++)
            {
                var hash = Hash(i, m - 1, a, b);
                result.Add(hash);
            }

            return result;
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Start Read facts");
            var facts = Reader.Read();

            Console.WriteLine("Start GroupBy UserId");
            var resultGroupByUserId = (from f in facts
                    group f by f.UserId
                    into g
                    select new {UserId = g.Key, Songs = g.Select(q => q.SongId).Distinct().ToArray()})
                .OrderBy(q => q.UserId).ToDictionary(q => q.UserId, q => q.Songs);

            // Pobranie maksymalnej wartości id piosenki
            var m = facts.GroupBy(q => q.SongId).Select(q => q.Key).Max();
            facts.Clear();

            // przechowuje b brandów po r wierszy
            var brands = new Dictionary<int, Dictionary<int, string>>();

            /*
             * przechowuje dodatkowo sygnatury wszystkich użytkowników
             * aby szybko wyliczać podobieństwo dla par kandydujących
             */
            var userSignatures = new Dictionary<int, int[]>();

            for (var i = 0; i < _b; i++)
            {
                // Kazdy użytkownik ma sygnature o długości r 
                var signatureBrand = new Dictionary<int, string>();

                for (var j = 0; j < _r; j++)
                {
                    var h = GetHashFunctions(m);

                    foreach (var user in resultGroupByUserId)
                    {
                        /*
                         * dla każdego użytkownika wyliczam minhash
                         * ustawiam min z funkcji haszującej dla pierwszej piosenki użytkownika
                         * sprawdzam pozostałe piosenki czy wartość hasza jest mniejsza od min (ustawiam nowe min)
                         */
                        var min = h[user.Value.First() - 1];
                        foreach (var song in user.Value)
                            if (h[song - 1] < min)
                                min = h[song - 1];

                        if (!signatureBrand.ContainsKey(user.Key))
                            signatureBrand.Add(user.Key, min.ToString());
                        else
                            signatureBrand[user.Key] += min;
                        //dodawanie minhasha do sygnatury użytkownika
                        if (!userSignatures.ContainsKey(user.Key))
                        {
                            var sig = new int[_r * _b];
                            sig[i * _r + j] = min;
                            userSignatures.Add(user.Key, sig);
                        }
                        else
                        {
                            userSignatures[user.Key][i * _r + j] = min;
                        }
                    }
                }
                brands[i] = signatureBrand;
                Console.WriteLine($"Calculate brand :{i + 1}/{_b}");
            }

            //pary kandydackie 
            var userCandidates = new Dictionary<int, List<int>>();
            var k = 1;

            foreach (var brand in brands)
            {
                //tworzenie koszyków
                var buckets = brand.Value.GroupBy(q => q.Value)
                    .ToDictionary(q => q.Key, q => q.Select(w => w.Key).ToList());

                // dodanie wszystkich par użytkowników którzy mieli tą samą sygnature w koszyku
                // jeśli jakaś para pojawi się w kilku koszykach pojawią się duplikaty, które są usuwane później

                foreach (var bucket in buckets)
                foreach (var user1 in bucket.Value)
                foreach (var user2 in bucket.Value)
                    if (!userCandidates.ContainsKey(user1))
                    {
                        var list = new List<int>();
                        list.Add(user2);
                        userCandidates.Add(user1, list);
                    }
                    else
                    {
                        userCandidates[user1].Add(user2);
                    }
                Console.WriteLine($"userCandidates brand :{k}/{_b}");
                k++;
            }
            brands.Clear();

            //końcowy rezultat
            var result = new Dictionary<int, List<KeyValuePair<int, string>>>();
            var l = 0;

            foreach (var c in userCandidates.OrderBy(q => q.Key))
            {
                var nearestNeighbours = new List<KeyValuePair<int, string>>();
                var user1 = c.Key;
                foreach (var user2 in c.Value.Distinct())
                {
                    //oszacowane prawdopodobieństwo
                    var approximateJaccard = ApproximateJaccard(userSignatures[user1], userSignatures[user2]);
                    //faktyczne prawdopodobieństwo
                    var trueJaccard = CalculateJaccard(resultGroupByUserId[user1], resultGroupByUserId[user2]);

                    nearestNeighbours.Add(new KeyValuePair<int, string>(user2,
                        $"{approximateJaccard:N3} {trueJaccard:N3}"));
                }
                result.Add(c.Key, nearestNeighbours.OrderByDescending(q => q.Value).ToList());
                //zapis do pliku
                if (result.Count % 10000 == 0)
                {
                    Save(result, l / 10000);
                    result.Clear();
                }
                Console.WriteLine(l);
                l++;
            }
            Save(result, l / 10000);
        }

        /// <summary>
        ///     Zapis do pliku
        /// </summary>
        /// <param name="result"></param>
        /// <param name="idFile"></param>
        private static void Save(Dictionary<int, List<KeyValuePair<int, string>>> result, int idFile)
        {
            if (!Directory.Exists("Result"))
                Directory.CreateDirectory("Result");
            using (var outputFile = File.AppendText($"Result\\Result{idFile}.txt"))
            {
                foreach (var user in result)
                {
                    outputFile.WriteLine($"User= {user.Key}");
                    foreach (var neighbour in user.Value)
                        outputFile.WriteLine($" {neighbour.Key} {neighbour.Value}");
                    outputFile.WriteLine(Environment.NewLine);
                }
            }
        }
    }
}