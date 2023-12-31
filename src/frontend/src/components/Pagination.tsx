import { useTranslation } from "react-i18next";
import Button from "./Button";

type Props =
{
    currentPage: number;
    onPageChange: (page: number) => void;
    pageSize: number;
    totalItems: number;
}

export default function Pagination(props: Props) 
{
    const currentPage = props.currentPage;
    const totalPages = props.pageSize > 0 ? Math.ceil(props.totalItems / props.pageSize) : 0;

    const showPrevious = currentPage > 1;
    const showNext = currentPage < totalPages;
    const showLeadingEllipses = currentPage > 3;
    const showTrailingEllipses = currentPage < totalPages - 2;
    const showFirstPageIndex = currentPage >= 1;
    const showPreviousPageIndex = currentPage > 2;
    const showCurrentPageIndex = currentPage > 1;
    const showNextPageIndex = currentPage < totalPages - 1;
    const showwLastPageIndex = currentPage < totalPages;

    const {t} = useTranslation();

    const onPageChange = props.onPageChange;

    const getPageIndexLink = (page: number) => <Button className={"idsk-button idsk-button--secondary" + (page === currentPage ? " nkod-current-page" : "")} style={{marginRight: '5px'}} onClick={() => onPageChange(page)}>
        {page}
    </Button>;

    return <>{totalPages > 0 ? <nav className="nkod-pagination">
        {showPrevious ? <Button className="idsk-button idsk-button--secondary nkod-previous-page" style={{marginRight: '5px'}} onClick={() => onPageChange(currentPage - 1)}>{t('previousPage')}</Button> : null}    
        {showFirstPageIndex ? getPageIndexLink(1) : null}
        {showLeadingEllipses ? <span className="nkod-pagination-ellipsis" style={{lineHeight: '34px', margin: '0 10px'}}>...</span> : null}
        {showPreviousPageIndex ? getPageIndexLink(currentPage - 1) : null}
        {showCurrentPageIndex ? getPageIndexLink(currentPage) : null}
        {showNextPageIndex ? getPageIndexLink(currentPage + 1) : null}
        {showTrailingEllipses ? <span className="nkod-pagination-ellipsis" style={{lineHeight: '34px', margin: '0 10px'}}>...</span> : null}
        {showwLastPageIndex ? getPageIndexLink(totalPages) : null}
        {showNext ? <Button className="idsk-button idsk-button--secondary nkod-next-page" onClick={() => onPageChange(currentPage + 1)}>{t('nextPage')}</Button> : null}
    </nav> : null}</>
}
