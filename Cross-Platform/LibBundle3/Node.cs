namespace LibBundle3 {
	public abstract class Node {
		public Node? Parent;
		public string Name;

		public Node(string name) {
			Name = name;
		}

		/// <summary>
		/// Get the absolute path in the tree
		/// </summary>
		public virtual string GetPath() {
			return Parent is null ? Name : Parent.GetPath() + Name;
		}
	}
}