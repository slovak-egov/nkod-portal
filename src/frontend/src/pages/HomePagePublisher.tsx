import { Link } from "react-router-dom";
import { Publisher } from "../client";
import { useTranslation } from "react-i18next";

type Props = {
    publisher: Publisher;
}

export default function HomePagePublisher(props: Props) {
    const themes = props.publisher.themes ? Object.entries(props.publisher.themes).filter(a => a[1] > 0).sort((a, b) => {
        const diff = b[1] - a[1];
        return diff === 0 ? a[0].localeCompare(b[0]) : diff;
    }).slice(0, 3).map(v => ({name: v[0], count: v[1]})) : [];
    const {t} = useTranslation();

    return <div className="idsk-crossroad__item " data-testid="publisher">
        <Link to={'/datasety?publisher=' + encodeURIComponent(props.publisher.key)} className="govuk-link idsk-crossroad-title" title={props.publisher.name} aria-hidden="false">{props.publisher.name}
        </Link>
        <p className="idsk-crossroad-subtitle" aria-hidden="false">{t('homePage.publisherDatasetCount', {val: props.publisher.datasetCount})}{themes.length > 0 ? <>, {t('homePage.themesMax')} {
            themes.map((t, i) => <span key={i}>{t.name} ({t.count}){i < themes.length - 1 ? ', ' : null}</span>)
        }.</> : null}
        </p>
        <hr className="idsk-crossroad-line" aria-hidden="true" />
    </div>
}