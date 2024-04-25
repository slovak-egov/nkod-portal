import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { useDocumentTitle } from '../client';
import { Application, useCmsApplications } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import SimpleList from '../components/SimpleList';
import { applicationTypeCodeList } from './ApplicationDetail';

const ApplicationList = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    useDocumentTitle(t('applicationList.headerTitle'));

    const [apps, loading, error, refresh] = useCmsApplications();
    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('applicationList.headerTitle') }]} />
            <MainContent>
                <div className="idsk-search-results__title">
                    <PageHeader size="l">{t('applicationList.title')}</PageHeader>
                </div>
                <GridRow data-testid="sr-add-new-row">
                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                        <Button onClick={() => navigate('/aplikacia/pridat')}>{t('addApplicationPage.new')}</Button>
                    </GridColumn>
                </GridRow>
                <SimpleList loading={loading} error={error} totalCount={apps?.length ?? 0}>
                    {apps?.map((app: Application, i: number) => (
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
                                            <CommentButton />
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
                                                {applicationTypeCodeList.find((type) => type.id === app.type)?.label}
                                            </span>
                                        </GridColumn>
                                    </>
                                )}
                            </GridRow>
                            {i < apps.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SimpleList>
            </MainContent>
        </>
    );
};

export default ApplicationList;
