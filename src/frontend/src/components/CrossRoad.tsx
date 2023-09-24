import { Link } from "react-router-dom";
import IdSkModule from "./IdSkModule";

type Props =
{
    items: CrossRoadItem[];
    variant: 1 | 2;
}

type CrossRoadItem =
{
    target: string;
    title: string;
    subTitle: string;
}

export default function Radio(props: Props)
{
    return <>
        {props.items.length > 0 && <IdSkModule moduleType="idsk-crossroad">
            <div className={'idsk-crossroad idsk-crossroad-' + props.variant}>
                {props.items.map(i => <div className="idsk-crossroad__item ">
                    <Link className="govuk-link idsk-crossroad-title" title={i.title} aria-hidden="false" to={i.target}>{i.title}</Link>
                    <p className="idsk-crossroad-subtitle" aria-hidden="false">{i.subTitle}</p>
                    <hr className="idsk-crossroad-line" aria-hidden="true" />
                </div>)}
            </div>
        </IdSkModule>}
    </>
}