import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { OrderOption, useDocumentTitle } from '../client';
import { Application, RequestCmsApplicationsQuery, useCmsApplicationsSearch } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import SearchResultsCms from '../components/SearchResultsCms';
import ApplicationListItem from './ApplicationListItem';

const ApplicationList = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    useDocumentTitle(t('applicationList.headerTitle'));

    const [apps, query, setQueryParameters, loading, error] = useCmsApplicationsSearch({
        orderBy: 'created'
    });

    const orderByOptions: OrderOption[] = [
        { name: t('byDateModified'), value: 'updated' },
        { name: t('byDateCreated'), value: 'created' },
        { name: t('byName'), value: 'title' },
        { name: t('byPopularity'), value: 'popularity' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('applicationList.headerTitle') }]} />
            <MainContent>
                <SearchResultsCms<RequestCmsApplicationsQuery>
                    header={t('applicationList.title')}
                    query={query}
                    customHeading={
                        <GridRow data-testid="sr-add-new-row">
                            <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                                <Button onClick={() => navigate('/aplikacia/pridat')}>{t('addApplicationPage.new')}</Button>
                            </GridColumn>
                        </GridRow>
                    }
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={apps?.paginationMetadata?.totalItemCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={['application-types', 'application-themes']}
                >
                    {apps?.items?.map((app: Application, i: number) => (
                        <ApplicationListItem key={i} app={app} isLast={i === apps?.items?.length - 1} />
                    ))}
                </SearchResultsCms>
            </MainContent>
        </>
    );
};

export default ApplicationList;
