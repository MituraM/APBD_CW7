using Cw3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public interface IDbService
    {
        public IEnumerable<Student> GetStudents();

        public Student GetStudent(string id);

        public Enrollment GetStudentEnrollment(string studentIndex);

        public Study GetStudy(string name);

        public Enrollment GetLatestEnrollment(string studyName, int semester);

        public Enrollment EnrollStudent(Student student, Study study);

        public Enrollment PromoteStudents(int idStudy, int semester);

        public Auth GetAuth(string id);

        public void SetPasswordHash(string id, string hash, string salt);

        public void UpdateRefreshToken(string id, string newToken);
    }
}
