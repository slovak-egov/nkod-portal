import { useTranslation } from "react-i18next";

type Props = {
    count: number;
}

export default function ResultsCount(props: Props) {
    const {t} = useTranslation();

    return <>
        {props.count} {props.count === 1 ? t('result1') : props.count > 1 && props.count < 5 ? t('results2-4') : t('results5')}
    </>
}