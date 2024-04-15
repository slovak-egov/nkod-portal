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
import { removeEntity, usePublishers, useDefaultHeaders, TokenContext, sendPost, TokenResult, useDocumentTitle } from '../client';
import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useContext } from 'react';
import { AxiosResponse } from 'axios';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import FormElementGroup from '../components/FormElementGroup';
import BaseInput from '../components/BaseInput';

export default function PublisherList() {
    const [publishers, query, setQueryParameters, loading, error, refresh] = usePublishers();
    const headers = useDefaultHeaders();
    const tokenContext = useContext(TokenContext);
    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('publishers'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publishers') }]} />
            <MainContent>
                <PageHeader>{t('publishers')}</PageHeader>

                <p>
                    <Button onClick={() => navigate('/sprava/poskytovatelia/pridat')}>{t('newPublisher')}</Button>
                </p>

                <FormElementGroup
                    label={t('search')}
                    element={(id) => <BaseInput id={id} value={query.queryText ?? ''} onChange={(e) => setQueryParameters({ queryText: e.target.value })} />}
                />

                {loading ? <Loading /> : null}
                {error ? <ErrorAlert error={error} /> : null}

                {publishers && publishers.items.length > 0 ? (
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
                                {publishers.items.map((p) => (
                                    <TableRow key={p.id}>
                                        <TableCell>{p.name}</TableCell>
                                        <TableCell>{p.isPublic ? t('published') : t('notPublished')}</TableCell>
                                        <TableCell>
                                            <Button
                                                className="idsk-button idsk-button--secondary"
                                                style={{ marginRight: '10px', marginBottom: '10px' }}
                                                onClick={() => navigate('/sprava/poskytovatelia/upravit/' + p.id)}
                                            >
                                                {t('edit')}
                                            </Button>

                                            <Button
                                                className="idsk-button idsk-button--secondary"
                                                style={{ marginRight: '10px', marginBottom: '10px' }}
                                                onClick={async () => {
                                                    const response: AxiosResponse<TokenResult> = await sendPost(
                                                        'publishers/impersonate?id=' + encodeURIComponent(p.id),
                                                        {},
                                                        headers
                                                    );
                                                    tokenContext?.setToken(response.data);
                                                    navigate('/');
                                                }}
                                            >
                                                {t('impersonate')}
                                            </Button>
                                            <Button
                                                className="idsk-button idsk-button--secondary"
                                                onClick={async () => {
                                                    await removeEntity(t('removePrompt'), 'publishers', p.id, headers);
                                                    refresh();
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
                            totalItems={publishers.totalCount}
                            pageSize={query.pageSize}
                            currentPage={query.page}
                            onPageChange={(p) => setQueryParameters({ page: p })}
                        />
                    </>
                ) : (
                    <div className="govuk-body">{t('publisherListEmpty')}</div>
                )}
            </MainContent>
        </>
    );
}
