/// Copyright 2022- Burak Kara, All rights reserved.
 
namespace WebServiceUtilities.PubSubUsers
{
    public abstract class PubSubAction
    {
        public abstract PubSubActions.EAction GetActionType();
        
        //Reminder for implementing static Action_[] DefaultInstance = new Action_[]();
        protected abstract PubSubAction GetStaticDefaultInstance();
    }
}