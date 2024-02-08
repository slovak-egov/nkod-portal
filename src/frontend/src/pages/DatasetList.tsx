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
import { removeDataset, useDatasets, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import ErrorAlert from '../components/ErrorAlert';
import { useNavigate } from 'react-router';
import Loading from '../components/Loading';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import AlertPublisher from '../components/AlertPublisher';
import Alert from '../components/Alert';
import { Link } from 'react-router-dom';

export default function DatasetList() {
    const [datasets, query, setQueryParameters, loading, error, refresh] = useDatasets({ pageSize: 20, page: 0 });
    const navigate = useNavigate();
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const [alert, setAlert] = useState<string | null>(null);

    useEffect(() => {
        if (userInfo?.publisher) {
            setQueryParameters({
                filters: {
                    publishers: [userInfo.publisher]
                },
                page: 1
            });
        }
    }, [userInfo, setQueryParameters]);
    const { t } = useTranslation();
    useDocumentTitle(t('datasetList'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('datasetList') }]} />

            <MainContent>
                <AlertPublisher />
                <PageHeader>{t('datasetList')}</PageHeader>

                {userInfo?.publisherView ? (
                    <p className="govuk-body nkod-publisher-name">
                        <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                        <br />
                        {userInfo.publisherView.name}
                    </p>
                ) : null}

                <p>
                    <Button onClick={() => navigate('/sprava/datasety/pridat')}>{t('newDataset')}</Button>
                    <Button style={{ marginLeft: '10px' }} onClick={() => navigate('/sprava/zmena-licencii')}>
                        {t('changeLicensesBulk')}
                    </Button>
                </p>

                {loading ? <Loading /> : null}
                {error ? <ErrorAlert error={error} /> : null}
                {alert ? (
                    <div className="custom-alert">
                        <Alert type="warning">
                            <div style={{ padding: '5px 10px' }}>{alert}</div>
                        </Alert>
                    </div>
                ) : null}

                {datasets ? (
                    <>
                        {datasets.items.length > 0 ? (
                            <>
                                <Table>
                                    <TableHead>
                                        <TableRow>
                                            <TableHeaderCell>{t('name')}</TableHeaderCell>
                                            <TableHeaderCell>{t('state')}</TableHeaderCell>
                                            <TableHeaderCell>{t('distributions')}</TableHeaderCell>
                                            <TableHeaderCell>{t('tools')}</TableHeaderCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {datasets.items.map((d) => (
                                            <TableRow key={d.id}>
                                                <TableCell>{d.name}</TableCell>
                                                <TableCell>{d.isPublic ? t('published') : t('notPublished')}</TableCell>
                                                <TableCell>
                                                    <div>
                                                        {d.distributions.map((distribution) => {
                                                            if (distribution.downloadUrl && distribution.formatValue) {
                                                                return (
                                                                    <a
                                                                        href={distribution.downloadUrl}
                                                                        className="govuk-link"
                                                                        key={distribution.id}
                                                                        style={{ marginRight: '0.5em' }}
                                                                    >
                                                                        {distribution.formatValue.label}
                                                                    </a>
                                                                );
                                                            }
                                                            return null;
                                                        })}
                                                    </div>
                                                    {!d.isSerie ? (
                                                        <Button
                                                            className="idsk-button idsk-button--secondary"
                                                            style={{ marginTop: '10px', marginBottom: '10px' }}
                                                            onClick={() => navigate('/sprava/distribucie/' + d.id)}
                                                        >
                                                            {t('editDistributions')}
                                                        </Button>
                                                    ) : null}
                                                </TableCell>
                                                <TableCell>
                                                    <Button
                                                        className="idsk-button idsk-button--secondary"
                                                        style={{ marginRight: '10px', marginTop: '10px', marginBottom: '10px' }}
                                                        onClick={() => navigate('/sprava/datasety/upravit/' + d.id)}
                                                    >
                                                        {t('edit')}
                                                    </Button>
                                                    <Button
                                                        className="idsk-button idsk-button--secondary"
                                                        style={{ marginTop: '10px', marginBottom: '10px' }}
                                                        onClick={async () => {
                                                            setAlert(null);
                                                            const r = await removeDataset(t('removePrompt'), d.id, headers);
                                                            if (r === true) {
                                                                refresh();
                                                            } else if (typeof r === 'string') {
                                                                setAlert(r);
                                                            }
                                                        }}
                                                    >
                                                        {t('remove')}
                                                    </Button>
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                                <Pagination
                                    totalItems={datasets.totalCount}
                                    pageSize={query.pageSize}
                                    currentPage={query.page}
                                    onPageChange={(p) => setQueryParameters({ page: p })}
                                />
                            </>
                        ) : (
                            <div className="govuk-body">{t('noDatasetsInList')}</div>
                        )}
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
