namespace LibBundle3 {
	public abstract class Node {
		public Node? Parent;
		public string Name;

		public Node(string name) {
			Name = name;
		}
	}
}