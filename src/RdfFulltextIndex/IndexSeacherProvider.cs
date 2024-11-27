using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFulltextIndex
{
    public class IndexSeacherProvider : IDisposable
    {
        private readonly object lockObj = new object();

        private int clientsCount;

        public IndexSeacherProvider(IndexWriter indexWriter, DirectoryTaxonomyWriter taxonomyWriter) 
        {
            IndexReader = indexWriter.GetReader(true);
            IndexSearcher = new IndexSearcher(IndexReader);
            TaxonomyReader = new DirectoryTaxonomyReader(taxonomyWriter);
        }

        public TaxonomyReader TaxonomyReader { get; }

        public IndexReader IndexReader { get; }

        public IndexSearcher IndexSearcher { get; }

        public bool ShouldBeDisposed { get; private set; }

        public void IncreaseClientsCount()
        {
            lock (lockObj)
            {
                clientsCount++;
            }
        }

        public void TryDispose()
        {
            lock (lockObj)
            {
                if (clientsCount <= 0)
                {
                    DisposeInternal();
                }
                else
                {
                    ShouldBeDisposed = true;
                }
            }
        }

        private void DisposeInternal()
        {
            IndexReader.Dispose();
            TaxonomyReader.Dispose();
            ShouldBeDisposed = false;
        }

        public void Dispose()
        {
            lock (lockObj)
            {
                clientsCount--;
                if (clientsCount <= 0 && ShouldBeDisposed)
                {
                    DisposeInternal();
                }
            }
        }
    }
}
