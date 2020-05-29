using Cw3.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public class SqlServerDbService : IDbService
    {
        public IEnumerable<Student> GetStudents()
        {
            var students = new List<Student>();

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"SELECT s.IndexNumber, s.FirstName, s.LastName, s.BirthDate, std.Name as StdName, e.Semester as ESemester
                                        FROM Student s
                                        INNER JOIN Enrollment e on s.IdEnrollment = e.IdEnrollment
                                        INNER JOIN Studies std on e.IdStudy = std.IdStudy;";
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var student = new Student
                    {
                        IndexNumber = reader["IndexNumber"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        BirthDate = DateTime.Parse(reader["BirthDate"].ToString()),
                        Course = reader["StdName"].ToString(),
                        Semester = int.Parse(reader["ESemester"].ToString())
                    };
                    students.Add(student);
                }

                reader.Close();
            }

            return students;
        }

        public Student GetStudent(string indexNumber)
        {
            Student student = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;

                try
                {
                    command.CommandText = @"SELECT s.IndexNumber, s.Password, s.Roles, s.FirstName, s.LastName, s.BirthDate, std.Name as StdName, e.Semester as ESemester
                                            FROM Student s
                                            INNER JOIN Enrollment e on s.IdEnrollment = e.IdEnrollment
                                            INNER JOIN Studies std on e.IdStudy = std.IdStudy
                                            WHERE s.IndexNumber = @indexNumber;";
                    command.Parameters.AddWithValue("indexNumber", indexNumber);

                    var reader = command.ExecuteReader();
                    reader.Read();

                    student = new Student
                    {
                        IndexNumber = reader["IndexNumber"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        BirthDate = DateTime.Parse(reader["BirthDate"].ToString()),
                        Course = reader["StdName"].ToString(),
                        Semester = int.Parse(reader["ESemester"].ToString())
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    student = null;
                }
            }

            return student;
        }

        public Enrollment GetStudentEnrollment(string studentIndex)
        {
            Enrollment enrollment = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;

                try
                {
                    command.CommandText = @"SELECT e.IdEnrollment, e.Semester, e.IdStudy, e.StartDate
                                            FROM Enrollment e
                                            INNER JOIN Student s ON s.IdEnrollment = e.IdEnrollment
                                            WHERE s.IndexNumber = @indexNumber;";
                    command.Parameters.AddWithValue("indexNumber", studentIndex);

                    var reader = command.ExecuteReader();
                    reader.Read();

                    enrollment = new Enrollment
                    {
                        IdEnrollment = int.Parse(reader["IdEnrollment"].ToString()),
                        Course = int.Parse(reader["IdStudy"].ToString()),
                        Semester = int.Parse(reader["Semester"].ToString()),
                        StartDate = DateTime.Parse(reader["StartDate"].ToString())
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    enrollment = null;
                }
            }

            return enrollment;
        }

        public Study GetStudy(string name)
        {
            Study study = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                try
                {
                    command.Connection = connection;
                    connection.Open();

                    command.CommandText = "SELECT IdStudy, Name FROM Studies WHERE name=@name";
                    command.Parameters.AddWithValue("name", name);
                    var reader = command.ExecuteReader();
                    reader.Read();

                    study = new Study
                    {
                        IdStudy = (int)reader["IdStudy"],
                        Name = reader["Name"].ToString()
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    study = null;
                }
            }

            return study;
        }

        public Enrollment GetLatestEnrollment(string studyName, int semester)
        {
            Enrollment enrollment = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                try
                {
                    command.Connection = connection;
                    connection.Open();

                    command.CommandText = @"SELECT TOP 1 e.IdEnrollment, e.Semester, e.IdStudy, e.StartDate FROM Enrollment e 
                                            INNER JOIN Studies s ON s.IdStudy = e.IdStudy 
                                            WHERE e.Semester = @semester AND s.Name = @name 
                                            ORDER BY e.StartDate DESC;";
                    command.Parameters.AddWithValue("semester", semester);
                    command.Parameters.AddWithValue("name", studyName);
                    var reader = command.ExecuteReader();
                    reader.Read();

                    enrollment = new Enrollment
                    {
                        IdEnrollment = (int)reader["IdEnrollment"],
                        Course = (int)reader["IdStudy"],
                        Semester = (int)reader["Semester"],
                        StartDate = DateTime.Parse(reader["StartDate"].ToString())
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    enrollment = null;
                }
            }

            return enrollment; 
        }

        public Enrollment EnrollStudent(Student student, Study study)
        {
            Enrollment enrollment = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                command.Connection = connection;
                command.Transaction = transaction;

                // Check if the enrollment exists
                try {
                    var SELECT_ENROLLMENT = @"SELECT TOP 1 IdEnrollment, Semester, IdStudy, StartDate 
                                              FROM Enrollment 
                                              WHERE IdStudy=@idStudy AND Semester=@semester 
                                              ORDER BY StartDate DESC;";      
                    command.CommandText = SELECT_ENROLLMENT;
                    command.Parameters.AddWithValue("semester", student.Semester);
                    command.Parameters.AddWithValue("idStudy", study.IdStudy);
                    var reader = command.ExecuteReader();
                    bool exists = reader.Read();

                    if (!exists)
                    {
                        reader.Close();

                        // If not exists, create a new one
                        command.CommandText = @"INSERT INTO Enrollment (IdEnrollment, Semester, IdStudy, StartDate) 
                                                SELECT MAX(IdEnrollment) + 1, @semester, @idStudy, @startDate 
                                                FROM Enrollment WITH (ROWLOCK, XLOCK, HOLDLOCK)";
                        command.Parameters.AddWithValue("startDate", DateTime.Now);
                        command.ExecuteNonQuery();

                        // Get the new value
                        command.CommandText = SELECT_ENROLLMENT;
                        var tmpReader = command.ExecuteReader();
                        tmpReader.Read();

                        enrollment = new Enrollment
                        {
                            IdEnrollment = (int)tmpReader["IdEnrollment"],
                            Semester = (int)tmpReader["Semester"],
                            StartDate = DateTime.Parse(tmpReader["StartDate"].ToString())
                        };

                        tmpReader.Close();
                    }
                    else
                    {
                        enrollment = new Enrollment
                        {
                            IdEnrollment = (int)reader["IdEnrollment"],
                            Semester = (int)reader["Semester"],
                            StartDate = DateTime.Parse(reader["StartDate"].ToString())
                        };

                        reader.Close();
                    }
                } 
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new Exception("Cannot create or read enrollment.");
                }

                // Check if the student's id is unique
                try
                {
                    command.CommandText = "SELECT IndexNumber FROM Student WHERE IndexNumber=@indexNumber";
                    command.Parameters.AddWithValue("indexNumber", student.IndexNumber);
                    var reader = command.ExecuteReader();
                    
                    if (reader.Read())
                    {
                        reader.Close();
                        transaction.Rollback();
                        throw new Exception("Student with the given ID already exists.");
                    }

                    reader.Close();
                }
                catch (SqlException)
                {
                    transaction.Rollback();
                    throw new Exception("Cannot read student.");
                }
                catch (Exception)
                {
                    throw;
                }

                // Enroll student
                try
                {
                    command.CommandText = @"INSERT INTO Student (IndexNumber, FirstName, LastName, BirthDate, IdEnrollment) 
                                            VALUES (@indexNumber, @firstName, @lastName, @birthDate, @idEnrollment)";
                    command.Parameters.AddWithValue("firstName", student.FirstName);
                    command.Parameters.AddWithValue("lastName", student.LastName);
                    command.Parameters.AddWithValue("birthDate", student.BirthDate);
                    command.Parameters.AddWithValue("idEnrollment", enrollment.IdEnrollment);
                    command.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new Exception("Cannot enroll student");
                }

                transaction.Commit();
            }

            return enrollment;
        }

        public Enrollment PromoteStudents(int idStudy, int semester)
        {
            Enrollment enrollment = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                command.Connection = connection;
                command.Transaction = transaction;

                // Execute procedure
                try
                {
                    command.CommandText = "[s19682].[PromoteStudents]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdStudy", idStudy);
                    command.Parameters.AddWithValue("@CurrentSemester", semester);

                    command.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new Exception("Cannot promote students");
                }

                // Get the new enrollment
                try
                {
                    command.CommandText = @"SELECT TOP 1 IdEnrollment, Semester, IdStudy, StartDate FROM Enrollment 
                                            WHERE Semester = @CurrentSemester + 1 AND IdStudy = @IdStudy 
                                            ORDER BY StartDate DESC;";
                    command.CommandType = CommandType.Text;
                    var reader = command.ExecuteReader();
                    reader.Read();

                    enrollment = new Enrollment
                    {
                        IdEnrollment = (int)reader["IdEnrollment"],
                        Course = (int)reader["IdStudy"],
                        Semester = (int)reader["Semester"],
                        StartDate = DateTime.Parse(reader["StartDate"].ToString())
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new Exception("Cannot get the new enrollment");
                }

                transaction.Commit();
            }

            return enrollment;
        }

        public Auth GetAuth(string id)
        {
            Auth authorization = null;

            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;

                try
                {
                    command.CommandText = @"SELECT IndexNumber, Password, Salt, Roles, RefreshToken
                                            FROM Student
                                            WHERE IndexNumber = @indexNumber;";
                    command.Parameters.AddWithValue("indexNumber", id);

                    var reader = command.ExecuteReader();
                    reader.Read();

                    authorization = new Auth
                    {
                        IndexNumber = reader["IndexNumber"].ToString(),
                        Password = reader["Password"].ToString(),
                        Salt = reader["Salt"].ToString(),
                        Roles = reader["Roles"].ToString().Split(","),
                        RefreshToken = reader["RefreshToken"].ToString()
                    };

                    reader.Close();
                }
                catch (Exception)
                {
                    authorization = null;
                }
            }

            return authorization;
        }

        public void SetPasswordHash(string id, string hash, string salt)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;

                command.CommandText = @"UPDATE Student
                                        SET Password = @hash, Salt = @salt
                                        WHERE IndexNumber = @indexNumber;";
                command.Parameters.AddWithValue("hash", hash);
                command.Parameters.AddWithValue("salt", salt);
                command.Parameters.AddWithValue("indexNumber", id);

                command.ExecuteNonQuery();
            }
        }


        public void UpdateRefreshToken(string id, string newToken)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;

                command.CommandText = @"UPDATE Student
                                        SET RefreshToken = @newToken
                                        WHERE IndexNumber = @indexNumber;";
                command.Parameters.AddWithValue("newToken", newToken);
                command.Parameters.AddWithValue("indexNumber", id);

                command.ExecuteNonQuery();
            }
        }
    }
}
