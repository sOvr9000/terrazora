
public abstract class Command {
	public abstract string Name { get; }
	public abstract string ExampleUsage { get; }

	// Optional: Flag if command requires server authority
	public virtual bool RequiresServerAuthority => false;

	// Optional: Flag if command should be networked
	public virtual bool IsNetworked => false;

	public abstract void Execute(string[] args);
}
