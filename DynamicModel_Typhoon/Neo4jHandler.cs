using System;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using FluentAssertions;
//using FluentAssertions;
using Neo4j.Driver;
//using Newtonsoft.Json;


namespace DynamicModel_Typhoon

{
    public class Neo4jHandler : IDisposable

    {
        public Neo4jHandler(string uri, string user, string password)
        {
            //建立基礎連線
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }
        public int AddEmployees(string companyName)
        {
            using (var session = _driver.Session())
            {
                var persons =
                    session.ReadTransaction(tx => tx.Run("MATCH (a:Person) RETURN a.name AS name").ToList());
                return persons.Sum(person => session.WriteTransaction(tx =>
                {
                    tx.Run("MATCH (emp:Person {name: $person_name}) " +
                           "MERGE (com:Company {name: $company_name}) " +
                           "MERGE (emp)-[:WORKS_FOR]->(com)",
                        new { person_name = ValueExtensions.As<string>(person["name"]), company_name = companyName });
                    return 1;
                }));
            }
        }

        protected virtual void Clear(bool isDisposing)
        {
            if (!isDisposing)
                return;

            using (var session = _driver.Session())
            {
                session.Run("MATCH (n) DETACH DELETE n").Consume();
            }
        }
        public void TestResultConsumeExample()
        {
            // Given
            Write("CREATE (a:Person {name: 'Alice'})");
            Write("CREATE (a:Person {name: 'Bob'})");
            // When & Then
            //GetPeople().Contains(new[] { "Alice", "Bob" });
        }
        private readonly IDriver _driver;
        public void CreateBySQL(string sql)
        {
            // Given
            Write(sql);
            //Write("CREATE (a:Person {name: 'Bob'})");
            // When & Then
            //GetPeople().Contains(new[] { "Alice", "Bob" });
        }
        public List<string> GetPeople()
        {
            using (var session = _driver.Session())
            {
                return session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (a:Person) RETURN a.name ORDER BY a.name");
                    return result.Select(record => ValueExtensions.As<string>(record[0])).ToList();// .As<string>())
                });
            }
        }
        public void PrintGreeting(string message)
        {
            using (var session = _driver.Session())
            {
                var greeting = session.WriteTransaction(tx =>
                {
                    string sql = "CREATE (a:Greeting) " +
                                        "SET a.message = 'message' " +
                                        "RETURN a.message + ', from node ' + id(a)";
                    var result = tx.Run(sql);
                    //var result = tx.Run("CREATE (a:Greeting) " +
                    //                    "SET a.message = $message " +
                    //                    "RETURN a.message + ', from node ' + id(a)",
                    //    new { message });
                    return ValueExtensions.As<string>(result.Single()[0]); //result.Single()[0].As<string>();
                });
                Console.WriteLine(greeting);
            }
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }
        public int CountNodes(string label, string property, string value)
        {
            using (var session = _driver.Session())
            {
                return session.ReadTransaction(
                    tx => tx.Run($"MATCH (a:{label} {{{property}: $value}}) RETURN count(a)",new { value }).Single()[0]
                    .As<int>());
            }
        }

        protected int CountPerson(string name)
        {
            return CountNodes("Person", "name", name);
        }

        protected void Write(string query, object parameters = null)
        {
            using (var session = _driver.Session())
            {
                session.WriteTransaction(tx =>
                    tx.Run(query, parameters));
            }
        }

        public List<IRecord> Read(string query, object parameters = null)
        {
            using (var session = _driver.Session())
            {
                return session.ReadTransaction(tx =>
                    tx.Run(query, parameters).ToList());
            }
        }

        private IResult AddCompany(ITransaction tx, string name)
        {
            return tx.Run("CREATE (a:Company {name: $name})", new { name });
        }

        // Create a person node
        private IResult AddPerson(ITransaction tx, string name)
        {
            return tx.Run("CREATE (a:Person {name: $name})", new { name });
        }

        // Create an employment relationship to a pre-existing company node.
        // This relies on the person first having been created.
        private IResult Employ(ITransaction tx, string personName, string companyName)
        {
            return tx.Run(@"MATCH (person:Person {name: $personName}) 
                         MATCH (company:Company {name: $companyName}) 
                         CREATE (person)-[:WORKS_FOR]->(company)", new { personName, companyName });
        }

        // Create a friendship between two people.
        private IResult MakeFriends(ITransaction tx, string name1, string name2)
        {
            return tx.Run(@"MATCH (a:Person {name: $name1}) 
                         MATCH (b:Person {name: $name2})
                         MERGE (a)-[:KNOWS]->(b)", new { name1, name2 });
        }

        // Match and display all friendships.
        private int PrintFriendships(ITransaction tx)
        {
            var result = tx.Run("MATCH (a)-[:KNOWS]->(b) RETURN a.name, b.name");

            var count = 0;
            foreach (var record in result)
            {
                count++;
                Console.WriteLine($"{record["a.name"]} knows {record["b.name"]}");
            }

            return count;
        }
        public void AddEmployAndMakeFriends()
        {
            // To collect the session bookmarks
            var savedBookmarks = new List<Bookmark>();

            // Create the first person and employment relationship.
            using (var session1 = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
            {
                session1.WriteTransaction(tx => AddCompany(tx, "Wayne Enterprises"));
                session1.WriteTransaction(tx => AddPerson(tx, "Alice"));
                session1.WriteTransaction(tx => Employ(tx, "Alice", "Wayne Enterprises"));

                savedBookmarks.Add(session1.LastBookmark);
            }

            // Create the second person and employment relationship.
            using (var session2 = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
            {
                session2.WriteTransaction(tx => AddCompany(tx, "LexCorp"));
                session2.WriteTransaction(tx => AddPerson(tx, "Bob"));
                session2.WriteTransaction(tx => Employ(tx, "Bob", "LexCorp"));

                savedBookmarks.Add(session2.LastBookmark);
            }

            // Create a friendship between the two people created above.
            using (var session3 = _driver.Session(o =>
                o.WithDefaultAccessMode(AccessMode.Write).WithBookmarks(savedBookmarks.ToArray())))
            {
                session3.WriteTransaction(tx => MakeFriends(tx, "Alice", "Bob"));

                session3.ReadTransaction(PrintFriendships);
            }
        }
        public void TestPassBookmarksExample()
        {
            // Given & When
            AddEmployAndMakeFriends();

            // Then
            CountNodes("Person", "name", "Alice");//.Should().Be(1);
            CountNodes("Person", "name", "Bob");//..Should().Be(1);
            CountNodes("Company", "name", "Wayne Enterprises");//..Should().Be(1);
            CountNodes("Company", "name", "LexCorp");//..Should().Be(1);

            var works1 = Read(
                "MATCH (a:Person {name: $person})-[:WORKS_FOR]->(b:Company {name: $company}) RETURN count(a)",
                new { person = "Alice", company = "Wayne Enterprises" });
            works1.Count();//..Should().Be(1);

            var works2 = Read(
                "MATCH (a:Person {name: $person})-[:WORKS_FOR]->(b:Company {name: $company}) RETURN count(a)",
                new { person = "Bob", company = "LexCorp" });
            works2.Count();//..Should().Be(1);

            var friends = Read(
                "MATCH (a:Person {name: $person1})-[:KNOWS]->(b:Person {name: $person2}) RETURN count(a)",
                new { person1 = "Alice", person2 = "Bob" });
            friends.Count();//..Should().Be(1);
        }

        private int ReadInt(string database, string query)
        {
            using (var session = _driver.Session(SessionConfigBuilder.ForDatabase(database)))
            {
                return ValueExtensions.As<int>(session.Run(query).Single()[0]);
            }
        }



        // tag::transaction-function[]
        public void AddPerson(string name)
        {
            using (var session = _driver.Session())
            {
                session.WriteTransaction(tx => tx.Run("CREATE (a:Person {name: $name})", new { name }));
            }
        }
        // end::transaction-function[]


        public void TestTransactionFunctionExample()
        {
            // Given & When
            AddPerson("Alice");
            // Then
            CountPerson("Alice");//..Should().Be(1);
        }

        // tag::session[]
        public void AddPerson2(string name)
        {
            using (var session = _driver.Session())
            {
                session.Run("CREATE (a:Person {name: $name})", new { name });
            }
        }
        // end::session[]


        public void TestSessionExample()
        {
            // Given & When
            AddPerson2("Alice");
            // Then
            CountPerson("Alice");//..Should().Be(1);
        }
    }
}
