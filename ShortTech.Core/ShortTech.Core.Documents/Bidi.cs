namespace ShortTech.Core.Documents;

public static class Bidi
{
	public static bool IsRightToLeft(string text)
	{
		int num = 0;
		while (num < text.Length)
		{
			char c = text[num];
			if (c >= 'יִ')
			{
				if (c <= 'ﻼ')
				{
					goto IL_0030;
				}
			}
			else if (c >= '\u0590' && c <= '\u08ff')
			{
				goto IL_0030;
			}
			bool flag = false;
			goto IL_0036;
			IL_0036:
			if (flag)
			{
				return true;
			}
			if (char.IsLetter(c))
			{
				return false;
			}
			num++;
			continue;
			IL_0030:
			flag = true;
			goto IL_0036;
		}
		return false;
	}
}
