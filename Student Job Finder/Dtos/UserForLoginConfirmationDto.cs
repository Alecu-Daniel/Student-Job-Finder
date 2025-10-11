namespace Student_Job_Finder.Dtos
{
    public class UserForLoginConfirmationDto
    {
        public byte[] PasswordHash { get; set; } = new byte[0];
        public byte[] PasswordSalt { get; set; } = new byte[0];
    }
}
