import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { OrderOption, useDocumentTitle } from '../client';
import { Application, RequestCmsApplicationsQuery, useCmsApplicationsSearch } from '../cms';
import { applicationTypeCodeList } from '../codelist/ApplicationCodelist';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import MainContent from '../components/MainContent';
import SearchResultsCms from '../components/SearchResultsCms';

const ApplicationList = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    useDocumentTitle(t('applicationList.headerTitle'));

    const [apps, query, setQueryParameters, loading, error] = useCmsApplicationsSearch({
        orderBy: 'title'
    });

    const orderByOptions: OrderOption[] = [
        { name: t('byDateModified'), value: 'updated' },
        { name: t('byDateCreated'), value: 'created' },
        { name: t('byName'), value: 'title' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('applicationList.headerTitle') }]} />
            <MainContent>
                <GridRow data-testid="sr-add-new-row">
                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                        <Button onClick={() => navigate('/aplikacia/pridat')}>{t('addApplicationPage.new')}</Button>
                    </GridColumn>
                </GridRow>

                <SearchResultsCms<RequestCmsApplicationsQuery>
                    header={t('applicationList.title')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={apps?.paginationMetadata?.totalItemCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={['application-types', 'application-themes']}
                >
                    {apps?.items?.map((app: Application, i: number) => (
                        <Fragment key={app.id}>
                            <GridRow data-testid="sr-result">
                                <GridColumn widthUnits={1} totalUnits={1}>
                                    <GridRow>
                                        <GridColumn widthUnits={1} totalUnits={2}>
                                            <Link to={'/aplikacia/' + app.id} className="idsk-card-title govuk-link">
                                                {app.title}
                                            </Link>
                                        </GridColumn>
                                        <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                            <Link to={`/aplikacia/${app.id}/upravit`} className="idsk-card-title govuk-link govuk-!-padding-right-3">
                                                {t('common.edit')}
                                            </Link>
                                            <LikeButton count={app.likeCount} contentId={app.id} url={`cms/applications/likes`} />
                                            <CommentButton count={app.commentCount} />
                                        </GridColumn>
                                    </GridRow>
                                </GridColumn>
                                {app.description && (
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <div
                                            style={{
                                                WebkitLineClamp: 3,
                                                WebkitBoxOrient: 'vertical',
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box'
                                            }}
                                        >
                                            {app.description}
                                        </div>
                                    </GridColumn>
                                )}
                                {app.type && (
                                    <>
                                        <GridColumn widthUnits={1} totalUnits={2}>
                                            <span style={{ color: '#000', fontWeight: 'bold' }}>
                                                {applicationTypeCodeList?.find((type) => type.id === app.type)?.label}
                                            </span>
                                        </GridColumn>
                                    </>
                                )}
                            </GridRow>
                            {i < apps?.items?.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SearchResultsCms>
            </MainContent>
        </>
    );
};

export default ApplicationList;
