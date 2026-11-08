namespace Pounce.Core
{
	record Section(List<string> Lines, int Line)
	{
		public string this [int index]
		{
			get => Lines[index];
		}

		public int Count
		{
			get => Lines.Count;
		}
	}
}