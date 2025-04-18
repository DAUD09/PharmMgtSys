using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PharmMgtSys.Models
{
    public class Notification
        {
        public int Id { get; set; }

        public string UserId { get; set; } // Links to the user receiving the notification

        public string Message { get; set; } // The alert message

        public bool IsRead { get; set; } // Tracks if the user has seen it

        public DateTime Timestamp { get; set; } // When the notification was created

        }
    
}
