using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FaceDesk_Bot.Permissions;

namespace FaceDesk_Bot
{
  public static class LocalExtensions
  {
    public static string WindowsToIana(string windowsZoneId)
    {
      // Avoid UTC being mapped to Etc/GMT, which is the mapping in CLDR
      if (windowsZoneId == "UTC")
      {
        return "Etc/UTC";
      }
      var source = NodaTime.TimeZones.TzdbDateTimeZoneSource.Default;
      string result = null;
      // If there's no such mapping, result will be null.
      source.WindowsMapping.PrimaryMapping.TryGetValue(windowsZoneId, out result);
      // Canonicalize
      if (result != null)
      {
        result = source.CanonicalIdMap[result];
      }
      return result;
    }

    public static async Task<bool> IsOwner(this SocketCommandContext context)
    {
      var application = await context.Client.GetApplicationInfoAsync();
      return (context.User.Id == application.Owner.Id ||
        SimplePermissions.Owners.Contains(context.User.Id));
    }

    public static string Mention(this ISocketMessageChannel channel)
    {
      return "<#" + channel.Id + ">";
    }

    public static T DeserializeObject<T>(string fileName)
    {
      T ret = default(T);
      XmlSerializer serializer = new XmlSerializer(typeof(T));

      using (FileStream fs = File.Create(fileName))
      {
        StreamReader reader = new StreamReader(fs);
        ret = (T) serializer.Deserialize(reader);
        reader.Dispose();
      }
      return ret;
    }

    public static void SerializeObject<T>(this T serializableObject, string fileName)
    {
      if (serializableObject == null) { return; }

      try
      {
        XDocument xmlDocument = new XDocument();
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        using (var writer = xmlDocument.CreateWriter())
        {
          xmlSerializer.Serialize(writer, serializableObject);
        }

        if (File.Exists(fileName))
        {
          File.Delete(fileName);
        }
        using (FileStream fs = File.Create(fileName))
        {
          fs.AddText(xmlDocument.ToString());
        }
      }
      catch (Exception ex)
      {
        //Log exception here
      }
    }

    private static void AddText(this FileStream fs, string value)
    {
      byte[] info = new UTF8Encoding(true).GetBytes(value);
      fs.Write(info, 0, info.Length);
    }

    public static int IndexOf(this Array arr, Object obj)
    {
      return Array.IndexOf(arr, obj);
    }

    public static DateTime ChangeTime(this DateTime dateTime, int hours, int minutes = -1, int seconds = -1, int milliseconds = -1)
    {
      if (minutes == -1) minutes = dateTime.Minute;
      if (seconds == -1) seconds = dateTime.Second;
      if (milliseconds == -1) milliseconds = dateTime.Millisecond;

      return new DateTime(
          dateTime.Year,
          dateTime.Month,
          dateTime.Day,
          hours,
          minutes,
          seconds,
          milliseconds,
          dateTime.Kind);
    }

    public static async Task<IUserMessage> DebugPublicReleasePrivate(
      this SocketCommandContext context, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null)
    {
      if (EntryPoint.Debug)
      {
        return await context.Channel.SendMessageAsync("(Not sending a message because in debug mode)\n" + text, isTTS, embed, options);
      }
      else
      {
        return await context.User.SendMessageAsync(text, isTTS, embed, options);
      }
    }

    public static string JoinArray<T>(this T[] array, string inBetween = ", ", string unlessContains = null)
    {
      // Concatenate all the elements into a StringBuilder.
      StringBuilder builder = new StringBuilder();
      foreach (var value in array)
      {
        if(unlessContains != null)
          if (value.ToString().Contains(unlessContains)) continue;

        
        builder.Append(value);
        builder.Append(inBetween);
      }
      builder.Remove(builder.Length - 2, 2);
      return builder.ToString();
    }
  }
}
