#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.AddressBook;
using U3DXT.iOS.Native.AddressBookUI;
using U3DXT.iOS.Native.Internals;
using U3DXT.iOS.Native.CoreFoundation;
using U3DXT.iOS.Native.CoreLocation;
using U3DXT.iOS.Native.EventKit;
using U3DXT.iOS.Native.EventKitUI;
using System.Collections.Generic;
using System.Linq;

namespace U3DXT.iOS.Personal {
	/// <summary>
	/// Main class for iOS Personal plugin. High-level API.</summary>
	/// <remarks>
	/// </remarks>
	public class PersonalXT {

#region events
		
		private static EventHandler<GrantedEventArgs> _calendarGrantedHandlers;
		/// <summary>
		/// Occurs when calendar granted.
		/// </summary>
		public static event EventHandler<GrantedEventArgs> CalendarAccess {
			add { _calendarGrantedHandlers += value; }
			remove { _calendarGrantedHandlers -= value; }
		}

		private static EventHandler<GrantedEventArgs> _reminderGrantedHandlers;
		/// <summary>
		/// Occurs when reminder granted.
		/// </summary>
		public static event EventHandler<GrantedEventArgs> ReminderAccess {
			add { _reminderGrantedHandlers += value; }
			remove { _reminderGrantedHandlers -= value; }
		}
		
		private static EventHandler<ReminderArgs> _remindersFoundHandlers;
		/// <summary>
		/// Occurs when reminders found.
		/// </summary>
		public static event EventHandler<ReminderArgs> RemindersFound {
			add { _remindersFoundHandlers += value; }
			remove { _remindersFoundHandlers -= value; }
		}
	

#endregion

#region vars
		/// <summary>
		/// The event store.
		/// </summary>
		public static EKEventStore eventStore;

		/// <summary>
		/// The calendar.
		/// </summary>
		public static EKCalendar calendar;

#endregion

#region methods
		/// <summary>
		/// Initializes the <see cref="U3DXT.iOS.Personal.PersonalXT"/> class.
		/// </summary>
		public static void Init()
		{
			eventStore = new EKEventStore();
			calendar = eventStore.defaultCalendarForNewEvents; //for the case where it is already granted
		}


		/// <summary>
		/// Requests the calendar access.
		/// </summary>
		public static void RequestCalendarAccess() 
		{
			eventStore.RequestAccessToEntityType(EKEntityType.Event, _RequestAccessToCalendarHandler);
		}

		/// <summary>
		/// _s the request access to calendar handler.
		/// </summary>
		/// <param name="granted">If set to <c>true</c> granted.</param>
		/// <param name="arg2">Arg2.</param>
		private static void _RequestAccessToCalendarHandler(bool granted, NSError arg2)
		{
			if(granted)				
				calendar = eventStore.defaultCalendarForNewEvents;

			if (_calendarGrantedHandlers != null)
				_calendarGrantedHandlers(null, new GrantedEventArgs(granted));
		}

		/// <summary>
		/// Requests the reminder access.
		/// </summary>
		public static void RequestReminderAccess() 
		{
			eventStore.RequestAccessToEntityType(EKEntityType.Reminder, _RequestAccessToReminderHandler);
		}

		private static void _RequestAccessToReminderHandler(bool granted, NSError arg2)
		{
			if (_reminderGrantedHandlers != null)
				_reminderGrantedHandlers(null, new GrantedEventArgs(granted));
		}
		
		/// <summary>
		/// _fetchs the reminders handler.
		/// </summary>
		/// <param name="objList">Object list.</param>
		private static void _fetchRemindersHandler(object[] objList)
		{
			if (_remindersFoundHandlers != null)
				_remindersFoundHandlers(null, new ReminderArgs(objList));
		}
		/// <summary>
		/// _removes the reminders.
		/// </summary>
		/// <param name="objList">Object list.</param>
		private static void _removeReminders(object[] objList)
		{
			EKReminder currentReminder;

			foreach(object obj  in objList)
			{
				currentReminder = obj as EKReminder;
				eventStore.RemoveReminder(currentReminder, true, null);
			}
			eventStore.SaveCalendar(calendar, true, null);
		}
		
		/// <summary>
		/// Gets the calendar access status.
		/// </summary>
		/// <returns>The calendar access status. Authorized, NotDetermined, StatusDenied, or StatusRestricted.</returns>
		public static string GetCalendarAccessStatus()
		{
			return EKEventStore.AuthorizationStatus(EKEntityType.Event).ToString();
		
		}

		/// <summary>
		/// Gets the reminder access status.
		/// </summary>
		/// <returns>The reminder access status.</returns>
		public static string GetReminderAccessStatus()
		{
			return EKEventStore.AuthorizationStatus(EKEntityType.Reminder).ToString();

		}

		/// <summary>
		/// Gets ABRecords on contacts
		/// </summary>
		public static ABRecord[] GetAllContactRecords()
		{
			NSError error = new NSError();
			var book = ABAddressBook.Create(null, error);
			var allPeople = book.CopyArrayOfAllPeople();

			return allPeople.Cast<ABRecord>().ToArray();

		}

		/// <summary>
		/// Gets all contact names in a form of string[]
		/// </summary>
		public static string[] GetAllContactNames()
		{
			NSError error = new NSError();
			var book = ABAddressBook.Create(null, error);
			var allPeople = book.CopyArrayOfAllPeople();
			string[] peopleNames = new string[allPeople.Length]; 

			for(int i=0; i < allPeople.Length; i++)
			{
				var cftype = allPeople[i] as CFType;
				var record = cftype.Cast<ABRecord>();
				peopleNames[i] = record.CopyCompositeName();
			}
			return peopleNames;
		}

		/// <summary>
		/// Gets event data in a specific date range
		/// </summary>
		public static object[] GetEventsFromTo(DateTime fromDate, DateTime toDate)
		{
			DateTime startDate = fromDate;
			DateTime endDate = toDate;
			object[] calenderArray = new object[1];
			calenderArray[0] = calendar;
			NSPredicate predicate = eventStore.PredicateForEvents(startDate, endDate, calenderArray);
			object[] eventObjs = eventStore.EventsMatchingPredicate(predicate);
          	eventStore.SaveCalendar(calendar,true, null);
			return eventObjs;
		}

		/// <summary>
		/// Creates the simple event.
		/// </summary>
		/// <param name="titleTxt">Title text.</param>
		/// <param name="fromDate">From date.</param>
		/// <param name="toDate">To date.</param>
		public static void CreateSimpleEvent(string titleTxt,DateTime fromDate, DateTime toDate )
		{
			EKEvent newEvent = EKEvent.Event(eventStore);		
		
			newEvent.title = titleTxt;
			newEvent.startDate = fromDate;
			newEvent.endDate = toDate;
			newEvent.calendar = calendar;

			eventStore.SaveEvent(newEvent, EKSpan.ThisEvent, null);
			eventStore.SaveCalendar(calendar,true, null);
		}

		/// <summary>
		/// Removes all events.
		/// </summary>
		public static void RemoveAllEvents()
		{
			object[] calenderArray = new object[1];
			calenderArray[0] = calendar;
			NSPredicate predicate = eventStore.PredicateForEvents(DateTime.Today, DateTime.Today.AddYears(1000), calenderArray);
			object[] eventObjs = eventStore.EventsMatchingPredicate(predicate);

			EKEvent eachEvent;
			foreach(object obj  in eventObjs)
			{
				eachEvent = obj as EKEvent;
				eventStore.RemoveEvent(eachEvent, EKSpan.ThisEvent, null);
			}
			eventStore.SaveCalendar(calendar, true, null);
		}

		/// <summary>
		/// Removes all reminders.
		/// </summary>
		public static void GetAllReminders()
		{
			object[] calenderArray = new object[1];
			calenderArray[0] = eventStore.defaultCalendarForNewReminders;
			
			NSPredicate predicate = eventStore.Predicate(calenderArray);
			eventStore.FetchRemindersMatchingPredicate(predicate,_fetchRemindersHandler); 

		}

		/// <summary>
		/// Removes all reminders.
		/// </summary>
		public static void RemoveAllReminders()
		{
			object[] calenderArray = new object[1];
			calenderArray[0] = eventStore.defaultCalendarForNewReminders;

			NSPredicate predicate = eventStore.Predicate(calenderArray);
			eventStore.FetchRemindersMatchingPredicate(predicate,_removeReminders); 

		}



#endregion

	}

}

#endif
