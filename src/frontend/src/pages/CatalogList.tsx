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
import { removeLocalCatalog, useDefaultHeaders, useDocumentTitle, useLocalCatalogs, useUserInfo } from '../client';
import { useNavigate } from 'react-router';
import { useEffect } from 'react';
import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useTranslation } from 'react-i18next';
import AlertPublisher from '../components/AlertPublisher';

export default function CatalogList() {
    const [catalogs, query, setQueryParameters, loading, error, refresh] = useLocalCatalogs({ pageSize: 20, page: 0 });
    const navigate = useNavigate();
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();

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
    useDocumentTitle(t('localCatalogList'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('localCatalogList') }]} />
            <MainContent>
                <AlertPublisher />
                <PageHeader>{t('localCatalogList')}</PageHeader>
                {userInfo?.publisherView ? (
                    <p className="govuk-body nkod-publisher-name">
                        <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                        <br />
                        {userInfo.publisherView.name}
                    </p>
                ) : null}
                <p>
                    <Button onClick={() => navigate('/sprava/lokalne-katalogy/pridat')}>{t('newCatalog')}</Button>
                </p>

                {loading ? <Loading /> : null}
                {error ? <ErrorAlert error={error} /> : null}

                {catalogs && catalogs.items.length > 0 ? (
                    <>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell>{t('name')}</TableHeaderCell>
                                    <TableHeaderCell>{t('state')}</TableHeaderCell>
                                    <TableHeaderCell>{t('tools')}</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {catalogs.items.map((c) => (
                                    <TableRow key={c.id}>
                                        <TableCell>{c.name}</TableCell>
                                        <TableCell>{c.isPublic ? t('published') : t('notPublished')}</TableCell>
                                        <TableCell style={{ whiteSpace: 'nowrap' }}>
                                            <Button
                                                className="idsk-button idsk-button--secondary"
                                                style={{ marginRight: '10px' }}
                                                onClick={() => navigate('/sprava/lokalne-katalogy/upravit/' + c.id)}
                                            >
                                                {t('edit')}
                                            </Button>
                                            <Button
                                                className="idsk-button idsk-button--secondary"
                                                onClick={async () => {
                                                    if (await removeLocalCatalog(t('removePrompt'), c.id, headers)) {
                                                        refresh();
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
                            totalItems={catalogs.totalCount}
                            pageSize={query.pageSize}
                            currentPage={query.page}
                            onPageChange={(p) => setQueryParameters({ page: p })}
                        />
                    </>
                ) : (
                    <div className="govuk-body">{t('catalogListEmpty')}</div>
                )}
            </MainContent>
        </>
    );
}
