namespace Pounce.Core
{
	record Section(List<string> Lines, int Offset)
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