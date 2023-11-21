using System;
using System.Linq;

namespace Insperex.EventHorizon.Abstractions.Models;

public class TopicData
{
    public string Id { get; }
    public string Topic { get; }
    public string NameSpace { get; set; }
    public DateTime CreatedDate { get; }

    public TopicData(string id, string topic, DateTime createdDate)
    {
        this.Id = id;
        this.Topic = topic.Split("/").Last();
        this.NameSpace = string.Join("/", topic.Split("/").Reverse().Skip(1).Reverse());
        this.CreatedDate = createdDate;
    }
}
