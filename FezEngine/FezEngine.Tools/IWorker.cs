namespace FezEngine.Tools;

internal interface IWorker
{
	void Act();

	void OnFinished();
}
