namespace CodeIndex.Common
{
	public class IndexStatusInfo
	{
		public IndexStatusInfo(IndexStatus indexStatus, IndexConfig indexConfig)
		{
			indexConfig.RequireNotNull(nameof(indexConfig));
			IndexStatus = indexStatus;
			IndexConfig = indexConfig;
		}

		public IndexStatus IndexStatus { get; set; }
		public IndexConfig IndexConfig { get; }
	}
}
