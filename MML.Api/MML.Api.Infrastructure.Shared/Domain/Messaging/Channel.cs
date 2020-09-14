﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Messenger.Core.Domain
{
    public class Channel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public virtual ICollection<ChatMessage> MessageHistory { get; set; }
    }
}
