namespace EduVS.Models
{
    public class AssignedStudentTestData
    {
        public StudentData Student { get; }
        public TestData Test { get; }

        public string DisplayText => $"{Student.DisplayName} -> #{Test.TestId}";

        public AssignedStudentTestData(StudentData student, TestData test)
        {
            Student = student;
            Test = test;
        }
    }
}
