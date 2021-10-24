using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class EpicData
    {
        public List<object> mutations { get; set; }
        public List<Query> queries { get; set; }
    }

    public class State
    {
        public object data { get; set; }
        public int dataUpdateCount { get; set; }
        public object dataUpdatedAt { get; set; }
        public object error { get; set; }
        public int errorUpdateCount { get; set; }
        public int errorUpdatedAt { get; set; }
        public int fetchFailureCount { get; set; }
        public object fetchMeta { get; set; }
        public bool isFetching { get; set; }
        public bool isInvalidated { get; set; }
        public bool isPaused { get; set; }
        public string status { get; set; }
    }

    public class Query
    {
        public State state { get; set; }
        public object queryKey { get; set; }
        public string queryHash { get; set; }
    }
}
