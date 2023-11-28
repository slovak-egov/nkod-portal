import { useEffect, useState } from "react";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Table from "../components/Table";
import TableHead from "../components/TableHead";
import TableRow from "../components/TableRow";
import TableHeaderCell from "../components/TableHeaderCell";
import TableBody from "../components/TableBody";
import TableCell from "../components/TableCell";
import PageSubheader from "../components/PageSubHeader";
import axios, { AxiosResponse } from "axios";
import Pagination from "../components/Pagination";
import { knownCodelists, useCodelists, useDocumentTitle, useEndpointUrl } from "../client";
import { useTranslation } from "react-i18next";

type Response = {
    results: {
        bindings: {[id:string] : any}[]
    };
}

async function runSparql(endpointUrl: string, query: string) {
    const response: AxiosResponse<Response> = await axios.get(endpointUrl + '?query=' + encodeURIComponent(query));
    return response.data;
}

const query1 = `PREFIX dcterms: <http://purl.org/dc/terms/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX foaf: <http://xmlns.com/foaf/0.1/>
PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
PREFIX dqv: <http://www.w3.org/ns/dqv#>
PREFIX qb: <http://purl.org/linked-data/cube#>
PREFIX sdmx-dimension: <http://purl.org/linked-data/sdmx/2009/dimension#>

PREFIX : <https://data.gov.sk/def/observation/data-quality/metrics/>

SELECT ?meno_poskytovateľa ((100 - MAX(?percento_distribúcií_s_podmienkami_0)) AS ?percento) (MAX(?počet_distribúcií_bez_podmienok_0) AS ?počet_distribúcií_bez_podmienok) (MAX(?počet_distribúcií_celkom_0) AS ?počet_distribúcií_celkom)
WHERE 
{
  <https://data.gov.sk/set/catalog/nkod> dcterms:modified ?date .
  BIND("http://reference.data.gov.uk/id/gregorian-day/" as ?datePrefix)
  BIND(IRI(CONCAT(?datePrefix, STR(?date))) AS ?perióda)
  
  OPTIONAL {?poskytovateľ foaf:name ?meno_poskytovateľa . }
  FILTER(BOUND(?meno_poskytovateľa) && langMatches(LANG(?meno_poskytovateľa), "sk"))

  {
    SELECT ?poskytovateľ ?perióda ?percento_distribúcií_s_podmienkami_0 (0 AS ?počet_distribúcií_bez_podmienok_0) (0 AS ?počet_distribúcií_celkom_0)
    WHERE {
      ?pozorováníProcento a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :ProcentoDistribucíDatovýchSadSPodmínkamiUžití ;
                          dqv:value ?percento_distribúcií_s_podmienkami_0 .

    }

  } UNION {
    SELECT ?poskytovateľ ?perióda (0 AS ?percento_distribúcií_s_podmienkami_0) ?počet_distribúcií_bez_podmienok_0 (0 AS ?počet_distribúcií_celkom_0)
    WHERE {
      ?pozorováníBezPodmínek a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :PočetDistribucíDatovýchSadBezPodmínekUžití ;
                          dqv:value ?počet_distribúcií_bez_podmienok_0 .

    }

  } UNION {
    SELECT ?poskytovateľ ?perióda (0 AS ?percento_distribúcií_s_podmienkami_0) (0 AS ?počet_distribúcií_bez_podmienok_0) ?počet_distribúcií_celkom_0
    WHERE {
      ?pozorovanieCelkom a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :PočetDistribucíDatovýchSad ;
                          dqv:value ?počet_distribúcií_celkom_0 .

    }

  }

}
GROUP BY ?meno_poskytovateľa
HAVING (?počet_distribúcií_bez_podmienok > 0)
ORDER BY DESC(?percento) DESC(?počet_distribúcií_bez_podmienok) ?meno_poskytovateľa
`;

const query2 = `PREFIX dcterms: <http://purl.org/dc/terms/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX foaf: <http://xmlns.com/foaf/0.1/>
PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
PREFIX dqv: <http://www.w3.org/ns/dqv#>
PREFIX qb: <http://purl.org/linked-data/cube#>
PREFIX sdmx-dimension: <http://purl.org/linked-data/sdmx/2009/dimension#>

PREFIX : <https://data.gov.sk/def/observation/data-quality/metrics/>

SELECT ?meno_poskytovateľa ((100 - MAX(?percento_datasetov_s_podmienkami_0)) AS ?percento) (MAX(?počet_datasetov_bez_podmienok_0) AS ?počet_datasetov_bez_podmienok) (MAX(?počet_datasetov_celkom_0) AS ?počet_datasetov_celkom)
WHERE 
{
  <https://data.gov.sk/set/catalog/nkod> dcterms:modified ?date .
  BIND("http://reference.data.gov.uk/id/gregorian-day/" as ?datePrefix)
  BIND(IRI(CONCAT(?datePrefix, STR(?date))) AS ?perióda)

  OPTIONAL {?poskytovateľ foaf:name ?meno_poskytovateľa . }
  FILTER(BOUND(?meno_poskytovateľa) && langMatches(LANG(?meno_poskytovateľa), "sk"))

  {
    SELECT ?poskytovateľ ?perióda ?percento_datasetov_s_podmienkami_0 (0 AS ?počet_datasetov_bez_podmienok_0) (0 AS ?počet_datasetov_celkom_0)
    WHERE {
      ?pozorováníProcento a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :ProcentoDatovýchSadSDistribucemiSPodmínkamiUžití ;
                          dqv:value ?percento_datasetov_s_podmienkami_0 .

    }

  } UNION {
    SELECT ?poskytovateľ ?perióda (0 AS ?percento_datasetov_s_podmienkami_0) ?počet_datasetov_bez_podmienok_0 (0 AS ?počet_datasetov_celkom_0)
    WHERE {
      ?pozorováníBezPodmínek a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :PočetDatovýchSadSDistribucíBezPodmínekUžití ;
                          dqv:value ?počet_datasetov_bez_podmienok_0 .
    }

  } UNION {
    SELECT ?poskytovateľ ?perióda (0 AS ?percento_datasetov_s_podmienkami_0) (0 AS ?počet_datasetov_bez_podmienok_0) ?počet_datasetov_celkom_0
    WHERE {
      ?pozorovanieCelkom a qb:Observation, dqv:QualityMeasurement ;
                          dqv:computedOn ?poskytovateľ ;
                          sdmx-dimension:refPeriod ?perióda ;
                          dqv:isMeasurementOf :PočetDatovýchSad ;
                          dqv:value ?počet_datasetov_celkom_0 .

    }

  }

}
GROUP BY ?meno_poskytovateľa
HAVING (?počet_datasetov_bez_podmienok > 0)
ORDER BY DESC(?percento) DESC(?počet_datasetov_bez_podmienok) ?meno_poskytovateľa
`; 

const query3 = `PREFIX dcterms: <http://purl.org/dc/terms/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX foaf: <http://xmlns.com/foaf/0.1/>
PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
PREFIX dqv: <http://www.w3.org/ns/dqv#>
PREFIX qb: <http://purl.org/linked-data/cube#>
PREFIX sdmx-dimension: <http://purl.org/linked-data/sdmx/2009/dimension#>

PREFIX : <https://data.gov.sk/def/observation/data-quality/metrics/>

SELECT DISTINCT ?meno_poskytovateľa ?počet_datasetov_nespĺňajúcich
WHERE 
{
  <https://data.gov.sk/set/catalog/nkod> dcterms:modified ?date .
  BIND("http://reference.data.gov.uk/id/gregorian-day/" as ?datePrefix)
  BIND(IRI(CONCAT(?datePrefix, STR(?date))) AS ?perióda)
      
  ?pozorovanie a qb:Observation, dqv:QualityMeasurement ;
    dqv:computedOn ?poskytovateľ ;
    sdmx-dimension:refPeriod ?perióda ;
    dqv:isMeasurementOf :PočetZáznamůDatovýchSadNesplňujícíchPovinnéAtributy ;
    dqv:value ?počet_datasetov_nespĺňajúcich .
  
  ?poskytovateľ foaf:name ?meno_poskytovateľa .
  FILTER(?počet_datasetov_nespĺňajúcich > 0)
}
ORDER BY DESC(?počet_datasetov_nespĺňajúcich) ?meno_poskytovateľa
`; 

const query4 = `PREFIX dcterms: <http://purl.org/dc/terms/>
PREFIX dcat:    <http://www.w3.org/ns/dcat#>
PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

SELECT ?mime_type (count (distinct ?distribúcia) as ?počet_distribúcií)
WHERE 
{
  ?dataset a dcat:Dataset ; 
  dcat:distribution ?distribúcia . 

  ?distribúcia dcat:mediaType ?mime_type.
}
GROUP BY ?mime_type 
ORDER BY DESC(?počet_distribúcií)
`; 

type QualityTableProps = {
    header: string;
    headerCells: string[];
    rows: {name: string, values: number[]}[];
}

function QualityTable(props: QualityTableProps) {
    const [page, setPage] = useState(1);
    const [orderBy, setOrderBy] = useState<number|null>(null);
    const [orderByDirection, setOrderByDirection] = useState<'asc'|'desc'>('asc');

    useEffect(() => {
        setPage(1);
    }, [orderBy, orderByDirection])

    const ordered = [...props.rows];
    const orderConstant = orderByDirection === 'asc' ? 1 : -1;
    if (orderBy !== null) {
        switch (orderBy) {
            case -1:
                ordered.sort((a, b) => a.name.localeCompare(b.name) * orderConstant);
                break;
            default:
                ordered.sort((a, b) => (a.values[orderBy] - b.values[orderBy]) * orderConstant);
                break;
        }
    }

    const pageSize = 10;
    const pageItems = ordered.slice((page - 1) * pageSize, page * pageSize);

    return <><PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>{props.header}</PageSubheader>
    <Table>
    <TableHead>
        <TableRow>
            {props.headerCells.map((c, i) => <TableHeaderCell key={i} enableSorting={true} sortingDirection={orderBy === i - 1 ? orderByDirection : null} toggleSortingDirection={
                () => {
                    if (orderBy === i - 1) {
                        if (orderByDirection === 'desc') {
                            setOrderBy(null);
                            setOrderByDirection('asc');
                        } else {
                            setOrderByDirection('desc');
                        }
                    } else {
                        setOrderBy(i - 1);
                        setOrderByDirection('asc');
                    }
                }
            }>{c}</TableHeaderCell>)}
        </TableRow>
    </TableHead>
    <TableBody>
        {pageItems.map((r, i) => <TableRow key={i}>
            <TableCell>{r.name}</TableCell>
            {r.values.map((v, j) => <TableCell key={j}>{v}</TableCell>)}
        </TableRow>)}
    </TableBody>
</Table>
<Pagination currentPage={page} totalItems={props.rows.length} pageSize={pageSize} onPageChange={setPage} /></>
}

const requiredCodelists = [knownCodelists.distribution.mediaType];

export default function Quality()
{
    const [results1, setResults1] = useState<Response|null>(null);
    const [results2, setResults2] = useState<Response|null>(null);
    const [results3, setResults3] = useState<Response|null>(null);
    const [results4, setResults4] = useState<Response|null>(null);

    const endpointUrl = useEndpointUrl();

    const [codelists] = useCodelists(requiredCodelists);
    const formatCodelist = codelists.find(c => c.id === knownCodelists.distribution.mediaType);
    const {t} = useTranslation();
    useDocumentTitle(t('metadataQuality'));

    useEffect(() => {
        async function load() {
            if (endpointUrl) {
              setResults1(await runSparql(endpointUrl, query1));
              setResults2(await runSparql(endpointUrl, query2));
              setResults3(await runSparql(endpointUrl, query3));
              setResults4(await runSparql(endpointUrl, query4));
            }
        }

        load();
    }, [endpointUrl]);

    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('metadataQuality')}]} />
            <MainContent>
                <PageHeader>{t('metadataQuality')}</PageHeader>

                {results1 ? <QualityTable header={t('distributionCountByPublisher')}
                                          headerCells={[t('publisher'), t('distributionsWithoutLicense'), t('totalCountDistributions')]}
                                          rows={results1.results.bindings.map(r => ({
                                            name: r['meno_poskytovateľa'].value,
                                            values: [
                                                r['počet_distribúcií_bez_podmienok'].value,
                                                r['počet_distribúcií_celkom'].value
                                            ]
                                          }))} /> : null}

                {results2 ? <QualityTable header={t('datasetsWithoutLicenseCount')}
                                          headerCells={[t('publisher'), t('datasetsWithoutLicense'), t('totalCountDatasets')]}
                                          rows={results2.results.bindings.map(r => ({
                                            name: r['meno_poskytovateľa'].value,
                                            values: [
                                                r['počet_datasetov_bez_podmienok'].value,
                                                r['počet_datasetov_celkom'].value
                                            ]
                                          }))} /> : null}


                {results3 ? <QualityTable header={t('datasetsCountWithoutRequiredAttributesByPublisher')}
                                          headerCells={[t('publisher'), t('datasetsCountWithoutRequiredAttributes')]}
                                          rows={results3.results.bindings.map(r => ({
                                            name: r['meno_poskytovateľa'].value,
                                            values: [
                                                r['počet_datasetov_nespĺňajúcich'].value
                                            ]
                                          }))} /> : null}

                {results4 && formatCodelist ? <QualityTable header={t('distributionsByFormat')}
                                          headerCells={[t('format'), t('totalCountDistributions')]}
                                          rows={results4.results.bindings.map(r => ({
                                            name: formatCodelist.values.find(v => v.id === r['mime_type'].value)?.label ?? r['mime_type'].value,
                                            values: [
                                                r['počet_distribúcií'].value
                                            ]
                                          }))} /> : null}

            </MainContent>
        </>;
}