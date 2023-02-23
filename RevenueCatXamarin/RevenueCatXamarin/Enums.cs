using System;

namespace RevenueCatXamarin
{
	public static class Enums
	{
		public static string EnumToTextValue ( Type enumType, int enumValue )
		// Returns name of enum value
		// Example: 
		// public enum WindRecording { Enabled, Disabled }
		// string s = Enums.EnumToValueText ( typeof(Enums.WindRecording), 1 )	// Returns "Disabled"
		{
			string[] names = Enum.GetNames( enumType );
			int[] values = (int[])Enum.GetValues ( enumType );
			for ( int i = 0; i < names.Length; i++ ) 
			{
				if ( values [i] == enumValue )
					return names [i];
			}

			return String.Empty;
		}
    }
}

