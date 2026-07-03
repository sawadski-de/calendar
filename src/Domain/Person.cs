namespace Domain;

public class Person
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public PersonRole Role { get; private set; }

    public Person(Guid id, string email, PersonRole role)
    {
        Id = id;
        Email = email;
        Role = role;
    }

    public void ChangeRole(PersonRole newRole) => Role = newRole;
}
