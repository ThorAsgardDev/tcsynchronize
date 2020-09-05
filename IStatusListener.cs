
namespace TCSynchronize
{
    interface IStatusListener
    {
        void onSynchronizing();
        void onListening();
        void onError();
    }
}
