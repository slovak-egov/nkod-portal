import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import GridRow from "../components/GridRow";
import GridColumn from "../components/GridColumn";
import { useLocalCatalog } from "../client";
import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";

export default function DetailLocalCatalog()
{
    const [catalog, loading, error] = useLocalCatalog();

    return  <>
        {loading ? <Loading /> : error ? <ErrorAlert error={error} /> : catalog ?
        <><Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Lokálne katalógy', link: '/lokalne-katalogy'}, {title: catalog.name}]} />
        <MainContent>
            <div className="nkod-entity-detail">
                <PageHeader>{catalog.name}</PageHeader>
                {catalog.publisher ? <p className="govuk-body nkod-publisher-name">
                    {catalog.publisher.name}
                </p> : null}
                {catalog.description ? <p className="govuk-body nkod-entity-description">
                    {catalog.description}
                </p> : null}
                <GridRow>
                    {catalog.publisher ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Poskytovateľ
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {catalog.publisher.name}
                            </div>
                        </div>                        
                    </GridColumn> : null}
                    {catalog.homePage ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Domáca stránka
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                <a href={catalog.homePage} className="govuk-link">Prejsť na domáciu stránku katalógu</a>
                            </div>
                        </div>                        
                    </GridColumn> : null}
                </GridRow>
            </div>
        </MainContent></> : null}
    </>
}