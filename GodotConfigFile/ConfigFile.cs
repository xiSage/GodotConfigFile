using System;

namespace GodotConfigFile
{
    public class ConfigFile
    {
        public Variant GetValue(string section, string key, Variant defaultValue = new Variant())
        {
            throw new NotImplementedException();
		}

        public bool HasSection(string section)
        {
            throw new NotImplementedException();
        }

        public bool HasSectionKey(string section, string key)
        {
            throw new NotImplementedException();
		}

        public string[] GetSections()
        {
            throw new NotImplementedException();
        }

        public string[] GetSectionKeys(string section)
        {
            throw new NotImplementedException();
		}

        public void Load(string path)
        {
            throw new NotImplementedException();
        }

        public void Parse(string data)
        {
            throw new NotImplementedException();
        }
	}
}
