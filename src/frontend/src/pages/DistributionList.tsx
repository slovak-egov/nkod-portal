import PageHeader from '../components/PageHeader';
import Button from '../components/Button';
import Table from '../components/Table';
import TableHead from '../components/TableHead';
import TableRow from '../components/TableRow';
import TableHeaderCell from '../components/TableHeaderCell';
import TableBody from '../components/TableBody';
import TableCell from '../components/TableCell';
import Pagination from '../components/Pagination';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import { useNavigate, useParams } from 'react-router';
import { removeDistribution, useDataset, useDefaultHeaders, useDistributions, useDocumentTitle, useUserInfo } from '../client';
import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useTranslation } from 'react-i18next';
import AlertPublisher from '../components/AlertPublisher';

export default function DistributionList() {
    const { datasetId } = useParams();
    const [distributions, query, setQueryParameters, loading, error, refresh] = useDistributions(
        datasetId ? { filters: { parent: [datasetId] } } : { page: 0 }
    );
    const [userInfo] = useUserInfo();
    const [dataset] = useDataset(datasetId);
    const navigate = useNavigate();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('distributionList'));

    return (
        <>
            <Breadcrumbs
                items={[{ title: t('nkod'), link: '/' }, { title: t('distributionList'), link: '/sprava/datasety' }, { title: t('distributionList') }]}
            />
            <MainContent>
                <AlertPublisher />
                <PageHeader>{t('distributionList')}</PageHeader>
                {userInfo?.publisherView ? (
                    <p className="govuk-body nkod-publisher-name">
                        <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                        <br />
                        {userInfo.publisherView.name}
                    </p>
                ) : null}
                {dataset ? (
                    <p className="govuk-body nkod-publisher-name">
                        <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('dataset')}</span>
                        <br />
                        {dataset.name}
                    </p>
                ) : null}

                {dataset && !dataset.isHarvested ? (
                    <p>
                        <Button onClick={() => navigate('/sprava/distribucie/' + datasetId + '/pridat')}>{t('newDistribution')}</Button>
                    </p>
                ) : null}

                {loading ? <Loading /> : null}
                {error ? <ErrorAlert error={error} /> : null}

                {distributions && distributions.items.length > 0 ? (
                    <>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell>{t('format')}</TableHeaderCell>
                                    <TableHeaderCell>{t('tools')}</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {distributions.items.map((d) => (
                                    <TableRow key={d.id}>
                                        <TableCell>
                                            {d.downloadUrl ? (
                                                <a href={d.downloadUrl} className="govuk-link">
                                                    {d.title ?? d.formatValue?.label ?? d.id}
                                                </a>
                                            ) : (
                                                <span></span>
                                            )}
                                        </TableCell>
                                        <TableCell style={{ whiteSpace: 'nowrap' }}>
                                            {d.downloadUrl ? (
                                                <Button
                                                    className="idsk-button idsk-button--secondary"
                                                    style={{ marginRight: '10px' }}
                                                    onClick={() => {
                                                        if (d.downloadUrl) {
                                                            window.location.href = d.downloadUrl;
                                                        }
                                                    }}
                                                >
                                                    {t('download')}
                                                </Button>
                                            ) : null}
                                            {!d.isHarvested ? (
                                                <>
                                                    <Button
                                                        className="idsk-button idsk-button--secondary"
                                                        style={{ marginRight: '10px' }}
                                                        onClick={() => navigate('/sprava/distribucie/' + datasetId + '/upravit/' + d.id)}
                                                    >
                                                        {t('edit')}
                                                    </Button>
                                                    <Button
                                                        className="idsk-button idsk-button--secondary"
                                                        onClick={async () => {
                                                            if (await removeDistribution(t('removePrompt'), d.id, headers)) {
                                                                refresh();
                                                            }
                                                        }}
                                                    >
                                                        {t('remove')}
                                                    </Button>
                                                </>
                                            ) : null}
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                        <Pagination
                            totalItems={distributions.totalCount}
                            pageSize={query.pageSize}
                            currentPage={query.page}
                            onPageChange={(p) => setQueryParameters({ page: p })}
                        />
                    </>
                ) : (
                    <div className="govuk-body">{t('datasetListEmpty')}</div>
                )}
            </MainContent>
        </>
    );
}
