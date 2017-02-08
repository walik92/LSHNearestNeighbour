using System.Collections.Generic;
using System.IO;
using LSHNearestNeighbour.Model;

namespace LSHNearestNeighbour
{
    public class Reader
    {
        private static readonly string _path = @"..\..\facts.csv";

        public static IList<Fact> Read()
        {
            var facts = new List<Fact>();
            using (var reader = new StreamReader(File.OpenRead(_path)))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    facts.Add(new Fact
                    {
                        SongId = int.Parse(values[0]),
                        UserId = int.Parse(values[1])
                    });
                }

                reader.Close();
            }

            //testowe dane 

            //facts.Add(new Fact() { UserId = 1, SongId = 1 });
            //facts.Add(new Fact() { UserId = 1, SongId = 2 });
            //facts.Add(new Fact() { UserId = 1, SongId = 3 });
            //facts.Add(new Fact() { UserId = 1, SongId = 4 });

            //facts.Add(new Fact() { UserId = 2, SongId = 1 });
            //facts.Add(new Fact() { UserId = 2, SongId = 4 });
            //facts.Add(new Fact() { UserId = 2, SongId = 5 });

            //facts.Add(new Fact() { UserId = 3, SongId = 4 });
            //facts.Add(new Fact() { UserId = 3, SongId = 5 });
            //facts.Add(new Fact() { UserId = 3, SongId = 6 });

            //facts.Add(new Fact() { UserId = 4, SongId = 2 });
            //facts.Add(new Fact() { UserId = 4, SongId = 3 });
            //facts.Add(new Fact() { UserId = 4, SongId = 5 });
            //facts.Add(new Fact() { UserId = 4, SongId = 6 });
            return facts;
        }
    }
}